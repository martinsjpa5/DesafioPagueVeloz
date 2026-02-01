import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-topside-bar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './topside-bar.component.html',
  styleUrls: ['./topside-bar.component.scss']
})
export class TopSideBarComponent {
  role = '';
  constructor(
    private auth: AuthService,
    private router: Router
  ) {
    this.auth.attToken();
    this.role = this.auth.role!;
  }

  get emailUsuario(): string | null {
    return this.auth.getUserEmail();
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
