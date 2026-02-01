export interface ApiGenericResponse<T> {
  successo: boolean;
  errors: string;
  data: T;
}

export interface ApiResponse {
  sucesso: boolean;
  errors: string;
}