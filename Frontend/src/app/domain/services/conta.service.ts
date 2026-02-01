import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiGenericResponse, ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment'
import { CriarTransacaoRequest } from '../models/criar-transacao-request.model';
import { ObterTransacaoResponse } from '../models/obter-transacao-response.model';
import { RegistrarContaRequest } from '../models/registrar-conta-request.model';
import { ObterClienteResponse } from '../models/obter-cliente-response.model';
import { ObterContaParaTransferenciaResponse } from '../models/obter-conta-para-transferencia-response.model';

@Injectable({
  providedIn: 'root'
})
export class ContaService {

  private readonly apiUrl = `${environment.apiUrl}/Conta`;

  constructor(private http: HttpClient) {}

  async Criar(request: RegistrarContaRequest): Promise<ApiResponse> {
    return await firstValueFrom(
      this.http.post<ApiResponse>(this.apiUrl + "/Registrar", request)
    );
  }

  async Obter(): Promise<ApiGenericResponse<ObterClienteResponse>> {
    return await firstValueFrom(this.http.get<ApiGenericResponse<ObterClienteResponse>>(this.apiUrl));
  }
  async ObterParaTransferencia(contaId: number): Promise<ApiGenericResponse<ObterContaParaTransferenciaResponse[]>> {
    return await firstValueFrom(this.http.get<ApiGenericResponse<ObterContaParaTransferenciaResponse[]>>(this.apiUrl + "/contasParaTransferencia/" + contaId));
  }

}
