import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { TopSideBarComponent } from './core/layouts/topsider-bar.component';

export const routes: Routes = [

  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/auth/login/login.component')
        .then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/auth/register/register.component')
        .then(m => m.RegisterComponent)
  },
  {
    path: '',
    component: TopSideBarComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'home',
        loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent)
      },
    ]
  },
  // {
  //   path: 'admin',
  //   component: TopSideBarComponent,
  //   canActivate: [AuthGuard],
  //   data: { roles: ['Gerente'] },
  //   loadChildren: () => import('./pages/admin/admin.routes').then(m => m.ADMIN_ROUTES) 
  // },
  

  { path: '**', redirectTo: 'login' }
];
