import { CommonModule, CurrencyPipe } from '@angular/common';
import { Component, TemplateRef, ViewChild, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { NgbModal, NgbModalModule, NgbTooltipModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxMaskDirective } from 'ngx-mask';

import { ContaService } from '../../domain/services/conta.service';
import { TransacaoService } from '../../domain/services/transacao.service';

import { CriarTransacaoRequest } from '../../domain/models/criar-transacao-request.model';
import { ObterClienteResponse } from '../../domain/models/obter-cliente-response.model';
import { ObterContaResponse } from '../../domain/models/obter-conta-response.model';
import { ObterTransacaoResponse } from '../../domain/models/obter-transacao-response.model';
import { RegistrarContaRequest } from '../../domain/models/registrar-conta-request.model';

import { LoadingService } from '../../core/services/loading.service';
import { ToastService } from '../../core/services/toast.service';
import { ApiErrorHelper } from '../../core/helpers/api-error.helper';
import { ObterContaParaTransferenciaResponse } from '../../domain/models/obter-conta-para-transferencia-response.model';

enum TipoOperacaoEnum {
  Credito = 1,
  Debito = 2,
  Reserva = 3,
  Captura = 4,
  Estorno = 5,
  Transferencia = 6,
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModalModule,
    NgxMaskDirective,
    CurrencyPipe,
    NgbTooltipModule,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly modal = inject(NgbModal);
  private readonly contaService = inject(ContaService);
  private readonly transacaoService = inject(TransacaoService);

  private readonly fb = inject(FormBuilder);
  private readonly loadingService = inject(LoadingService);
  private readonly toastService = inject(ToastService);

  @ViewChild('modalCadastrarConta', { static: true }) modalCadastrarConta!: TemplateRef<any>;
  @ViewChild('modalCriarTransacao', { static: true }) modalCriarTransacao!: TemplateRef<any>;

  cliente: ObterClienteResponse | null = null;

  contas: number[] = [];
  contaSelecionadaId: number | null = null;

  contaSelecionadaDetalhe: ObterContaResponse | null = null;

  transacoes: ObterTransacaoResponse[] = [];

  formConta!: FormGroup;
  formTransacao!: FormGroup;

  isRefreshing = false;

  contasDestino: ObterContaParaTransferenciaResponse[] = [];
  carregandoContasDestino = false;

  transacoesPassiveisDeEstorno: ObterTransacaoResponse[] = [];
  carregandoTransacoesEstorno = false;

  readonly operacoes = [
    { value: TipoOperacaoEnum.Credito, label: 'Crédito' },
    { value: TipoOperacaoEnum.Debito, label: 'Débito' },
    { value: TipoOperacaoEnum.Reserva, label: 'Reserva' },
    { value: TipoOperacaoEnum.Captura, label: 'Captura' },
    { value: TipoOperacaoEnum.Estorno, label: 'Estorno' },
    { value: TipoOperacaoEnum.Transferencia, label: 'Transferência' },
  ];

  get contaSelecionada(): ObterContaResponse | null {
    return this.contaSelecionadaDetalhe;
  }

  get operacaoAtual(): number {
    return Number(this.formTransacao?.get('operacao')?.value ?? 0);
  }

  get isTransferencia(): boolean {
    return this.operacaoAtual === TipoOperacaoEnum.Transferencia;
  }

  get isEstorno(): boolean {
    return this.operacaoAtual === TipoOperacaoEnum.Estorno;
  }

  async ngOnInit(): Promise<void> {
    this.buildForms();
    await this.carregarContas();
  }

  private positiveNumberExceptEstornoValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const operacao = Number(this.formTransacao?.get('operacao')?.value);
      const valor = Number(control.value);

      if (operacao === TipoOperacaoEnum.Estorno) return null;

      if (!Number.isFinite(valor) || valor <= 0) return { maiorQueZero: true };

      return null;
    };
  }

  private buildForms(): void {
    this.formConta = this.fb.group({
      saldoInicial: [0, [Validators.required]],
      limiteDeCredito: [0, [Validators.required]],
    });

    this.formTransacao = this.fb.group({
      operacao: [TipoOperacaoEnum.Credito, [Validators.required]],
      quantia: [0, [Validators.required, this.positiveNumberExceptEstornoValidator()]],
      moeda: ['BRL', [Validators.required]],
      contaDestinoId: [null],
      transacaoEstornadaId: [null],
    });

    this.formTransacao.get('operacao')?.valueChanges.subscribe(async () => {
      this.applyOperacaoValidators();

      if (this.isTransferencia) {
        this.transacoesPassiveisDeEstorno = [];
        this.formTransacao.patchValue({ transacaoEstornadaId: null }, { emitEvent: false });

        await this.carregarContasDestinoParaTransferencia();
        return;
      }

      if (this.isEstorno) {
        this.contasDestino = [];
        this.formTransacao.patchValue({ contaDestinoId: null }, { emitEvent: false });

        await this.carregarTransacoesPassiveisDeEstorno();
        return;
      }

      this.contasDestino = [];
      this.transacoesPassiveisDeEstorno = [];
      this.formTransacao.patchValue(
        { contaDestinoId: null, transacaoEstornadaId: null },
        { emitEvent: false }
      );
    });

    this.applyOperacaoValidators();
  }

  private applyOperacaoValidators(): void {
    const operacao = Number(this.formTransacao.get('operacao')?.value);

    const contaDestinoCtrl = this.formTransacao.get('contaDestinoId');
    const transacaoEstornadaCtrl = this.formTransacao.get('transacaoEstornadaId');
    const quantiaCtrl = this.formTransacao.get('quantia');

    contaDestinoCtrl?.clearValidators();
    transacaoEstornadaCtrl?.clearValidators();

    if (operacao === TipoOperacaoEnum.Transferencia) {
      contaDestinoCtrl?.setValidators([Validators.required]);
    }

    if (operacao === TipoOperacaoEnum.Estorno) {
      transacaoEstornadaCtrl?.setValidators([Validators.required]);
    }

    contaDestinoCtrl?.updateValueAndValidity({ emitEvent: false });
    transacaoEstornadaCtrl?.updateValueAndValidity({ emitEvent: false });

    quantiaCtrl?.updateValueAndValidity({ emitEvent: false });
  }

  private normalizeNumber(v: any): number {
    const n = Number(v);
    return Number.isFinite(n) ? n : 0;
  }

  formatarTransacaoParaEstorno(t: ObterTransacaoResponse): string {
    const parts: string[] = [];

    parts.push(`#${(t as any).id ?? '-'}`);

    if ((t as any).tipo != null) parts.push(`Tipo: ${(t as any).tipo}`);
    if ((t as any).status != null) parts.push(`Status: ${(t as any).status}`);

    const quantia = (t as any).quantia;
    const moeda = (t as any).moeda;
    if (quantia != null || moeda != null) parts.push(`${quantia ?? '-'} ${moeda ?? ''}`.trim());

    const contaDestinoId = (t as any).contaDestinoId;
    const nomeClienteDestino = (t as any).nomeClienteContaDestino;
    if (contaDestinoId != null) parts.push(`Dest: ${contaDestinoId}`);
    if (nomeClienteDestino) parts.push(`Cliente: ${nomeClienteDestino}`);

    return parts.join(' | ');
  }

  async carregarContas(): Promise<void> {
    this.loadingService.show();

    try {
      const resp = await this.contaService.Obter();
      this.cliente = resp?.data ?? null;

      this.contas = this.cliente?.contas ?? [];

      if (this.contaSelecionadaId && !this.contas.includes(this.contaSelecionadaId)) {
        this.contaSelecionadaId = null;
        this.contaSelecionadaDetalhe = null;
        this.transacoes = [];
      }
    } catch (error) {
      this.cliente = null;
      this.contas = [];
      this.contaSelecionadaId = null;
      this.contaSelecionadaDetalhe = null;
      this.transacoes = [];
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
    }
  }

  async onSelecionarConta(): Promise<void> {
    if (!this.contaSelecionadaId) {
      this.contaSelecionadaDetalhe = null;
      this.transacoes = [];
      return;
    }

    await this.carregarContaSelecionada(this.contaSelecionadaId);
    await this.carregarTransacoes(this.contaSelecionadaId);
  }

  async carregarContaSelecionada(contaId: number): Promise<void> {
    this.loadingService.show();

    try {
      const resp = await this.contaService.ObterPorId(contaId);
      this.contaSelecionadaDetalhe = resp?.data ?? null;
    } catch (error) {
      this.contaSelecionadaDetalhe = null;
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
    }
  }

  async carregarTransacoes(contaId: number): Promise<void> {
    this.loadingService.show();

    try {
      const resp = await this.transacaoService.ObterTransacoes(contaId);
      this.transacoes = resp?.data ?? [];
    } catch (error) {
      this.transacoes = [];
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
    }
  }

  async carregarContasDestinoParaTransferencia(): Promise<void> {
    if (!this.contaSelecionadaId) {
      this.contasDestino = [];
      this.formTransacao.patchValue({ contaDestinoId: null }, { emitEvent: false });
      return;
    }

    this.carregandoContasDestino = true;

    try {
      const resp = await this.contaService.ObterParaTransferencia(this.contaSelecionadaId);
      this.contasDestino = resp?.data ?? [];

      const atual = this.formTransacao.get('contaDestinoId')?.value;
      if (atual && !this.contasDestino.some((x) => x.id === Number(atual))) {
        this.formTransacao.patchValue({ contaDestinoId: null }, { emitEvent: false });
      }
    } catch (error) {
      this.contasDestino = [];
      this.formTransacao.patchValue({ contaDestinoId: null }, { emitEvent: false });
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.carregandoContasDestino = false;
    }
  }

  async carregarTransacoesPassiveisDeEstorno(): Promise<void> {
    if (!this.contaSelecionadaId) {
      this.transacoesPassiveisDeEstorno = [];
      this.formTransacao.patchValue({ transacaoEstornadaId: null }, { emitEvent: false });
      return;
    }

    this.carregandoTransacoesEstorno = true;

    try {
      const resp = await this.transacaoService.ObterTransacoesPassiveisDeEstorno(this.contaSelecionadaId);
      this.transacoesPassiveisDeEstorno = resp?.data ?? [];

      const atual = this.formTransacao.get('transacaoEstornadaId')?.value;
      if (atual && !this.transacoesPassiveisDeEstorno.some((x) => (x as any).id === Number(atual))) {
        this.formTransacao.patchValue({ transacaoEstornadaId: null }, { emitEvent: false });
      }
    } catch (error) {
      this.transacoesPassiveisDeEstorno = [];
      this.formTransacao.patchValue({ transacaoEstornadaId: null }, { emitEvent: false });
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.carregandoTransacoesEstorno = false;
    }
  }

  async atualizarDados(toast: boolean = false): Promise<void> {
    if (this.isRefreshing) return;

    this.isRefreshing = true;
    this.loadingService.show();

    try {
      const resp = await this.contaService.Obter();
      this.cliente = resp?.data ?? null;
      this.contas = this.cliente?.contas ?? [];

      if (this.contaSelecionadaId && !this.contas.includes(this.contaSelecionadaId)) {
        this.contaSelecionadaId = null;
        this.contaSelecionadaDetalhe = null;
        this.transacoes = [];
        if (toast) this.toastService.success('Dados atualizados!');
        return;
      }

      if (this.contaSelecionadaId) {
        const contaResp = await this.contaService.ObterPorId(this.contaSelecionadaId);
        this.contaSelecionadaDetalhe = contaResp?.data ?? null;

        const txResp = await this.transacaoService.ObterTransacoes(this.contaSelecionadaId);
        this.transacoes = txResp?.data ?? [];
      } else {
        this.contaSelecionadaDetalhe = null;
        this.transacoes = [];
      }

      if (this.isTransferencia) {
        await this.carregarContasDestinoParaTransferencia();
      }
      if (this.isEstorno) {
        await this.carregarTransacoesPassiveisDeEstorno();
      }

      if (toast) this.toastService.success('Dados atualizados!');
    } catch (error) {
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
      this.isRefreshing = false;
    }
  }

  abrirModalCadastroConta(): void {
    this.formConta.reset({ saldoInicial: 0, limiteDeCredito: 0 });
    this.modal.open(this.modalCadastrarConta, { centered: true, size: 'md' });
  }

  async salvarConta(modalRef: any): Promise<void> {
    if (this.formConta.invalid) {
      this.formConta.markAllAsTouched();
      return;
    }

    this.loadingService.show();

    try {
      const payload: RegistrarContaRequest = {
        saldoInicial: this.normalizeNumber(this.formConta.value.saldoInicial),
        limiteDeCredito: this.normalizeNumber(this.formConta.value.limiteDeCredito),
      };

      await this.contaService.Criar(payload);
      modalRef.close();

      this.toastService.success('Conta cadastrada com sucesso!');
      await this.atualizarDados();
    } catch (error) {
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
    }
  }

  async abrirModalCriarTransacao(): Promise<void> {
    if (!this.contaSelecionadaId) {
      this.toastService.error('Selecione uma conta de origem antes de criar uma transação.');
      return;
    }

    this.formTransacao.reset({
      operacao: TipoOperacaoEnum.Credito,
      quantia: 0,
      moeda: 'BRL',
      contaDestinoId: null,
      transacaoEstornadaId: null,
    });

    this.applyOperacaoValidators();

    this.contasDestino = [];
    this.transacoesPassiveisDeEstorno = [];

    this.modal.open(this.modalCriarTransacao, { centered: true, size: 'lg' });
  }

  async salvarTransacao(modalRef: any): Promise<void> {
    if (this.formTransacao.invalid) {
      this.formTransacao.markAllAsTouched();
      return;
    }

    if (!this.contaSelecionadaId) {
      this.toastService.error('Conta de origem não selecionada.');
      return;
    }

    const operacao = this.normalizeNumber(this.formTransacao.value.operacao);
    const quantia = this.normalizeNumber(this.formTransacao.value.quantia);

    // ✅ reforço no submit (defesa extra)
    if (operacao !== TipoOperacaoEnum.Estorno && quantia <= 0) {
      this.toastService.error('A quantia deve ser maior que zero para esta operação.');
      return;
    }

    this.loadingService.show();

    try {
      const payload: CriarTransacaoRequest = {
        contaOrigemId: this.contaSelecionadaId,
        operacao,
        quantia,
        moeda: this.formTransacao.value.moeda,
        contaDestinoId: this.formTransacao.value.contaDestinoId ?? null,
        transacaoEstornadaId: this.formTransacao.value.transacaoEstornadaId ?? null,
      };

      await this.transacaoService.Criar(payload);
      modalRef.close();

      this.toastService.success('Transação criada com sucesso!');
      await this.atualizarDados();
    } catch (error) {
      this.toastService.error(ApiErrorHelper.getApiErrorMessage(error));
    } finally {
      this.loadingService.hide();
    }
  }
}
