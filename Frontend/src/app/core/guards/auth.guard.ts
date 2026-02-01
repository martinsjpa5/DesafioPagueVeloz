import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(
    private auth: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {

    const rolesPermitidas = route.data['roles'] as string[] | undefined;

    if (rolesPermitidas && !rolesPermitidas.includes(this.auth.role!)) {
      this.router.navigate(['/vitrine']);
      return false;
    }


    if (this.auth.isLogged()) {
      return true;
    }

    this.router.navigate(['/login']);
    return false;
  }
}
