import { ObterContaResponse } from "./obter-conta-response.model";

export interface ObterClienteResponse{
    nome:string,
    contas:ObterContaResponse[]
}