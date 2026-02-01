import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiGenericResponse, ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment'
import { CriarTransacaoRequest } from '../models/criar-transacao-request.model';
import { ObterTransacaoResponse } from '../models/obter-transacao-response.model';

@Injectable({
  providedIn: 'root'
})
export class TransacaoService {

  private readonly apiUrl = `${environment.apiUrl}/Transacao`;

  constructor(private http: HttpClient) {}

  async Criar(request: CriarTransacaoRequest): Promise<ApiResponse> {
    return await firstValueFrom(
      this.http.post<ApiResponse>(this.apiUrl, request)
    );
  }

  async ObterTransacoesPassiveisDeEstorno(contaId: number): Promise<ApiGenericResponse<ObterTransacaoResponse[]>> {
    return await firstValueFrom(this.http.get<ApiGenericResponse<ObterTransacaoResponse[]>>(this.apiUrl + "/passiveisDeEstorno/conta/" + contaId ));
  }
  async ObterTransacoes(contaId: number): Promise<ApiGenericResponse<ObterTransacaoResponse[]>> {
    return await firstValueFrom(this.http.get<ApiGenericResponse<ObterTransacaoResponse[]>>(this.apiUrl + "/conta/" + contaId));
  }

}
