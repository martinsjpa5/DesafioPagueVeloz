import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, tap } from 'rxjs';
import { environment } from '../../../environments/environment'
import { LoginResponse } from '../../domain/models/login-response.model';
import { ApiGenericResponse } from '../../domain/models/api-response.model';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = `${environment.apiUrl}/Auth`;
  private tokenKey = 'auth_token';
  private emailKey = 'auth_email';

  public role: string | undefined;

  constructor(private http: HttpClient) {
    this.attToken();
   }

   attToken(){
    if(this.getToken()){
      this.getRole();
    }
   }

  async login(email: string, senha: string): Promise<ApiGenericResponse<LoginResponse>> {
    const result = await firstValueFrom(
      this.http.post(`${this.apiUrl}/Logar`, { email, senha }).pipe(
        tap((res: any) => {
          if (res?.data) {
            this.saveToken(res.data);
          }
        })
      )
    );

    return result;
  }

  async register(data: any): Promise<any> {
    return await firstValueFrom(
      this.http.post(`${this.apiUrl}/registrar`, data)
    );
  }

  saveToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  removeToken(): void {
    localStorage.removeItem(this.tokenKey);
  }

  isLogged(): boolean {
    return this.getToken() !== null;
  }

  logout(): void {
    this.role = '';
    this.removeUserEmail();
    this.removeToken();
  }

  saveUserEmail(email: string) {
    localStorage.setItem(this.emailKey, email);
  }

  getUserEmail(): string | null {
    return localStorage.getItem(this.emailKey);
  }

  removeUserEmail(): void {
    localStorage.removeItem(this.emailKey);
  }

  private getRole(): void{
    const token = localStorage.getItem(this.tokenKey);
    if (!token) return;

    try {
      const payload: any = jwtDecode(token);
      this.role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
    } catch {
      return;
    }
  }
}
