import { Injectable } from '@angular/core';
import { ToastrService, IndividualConfig } from 'ngx-toastr';

@Injectable({ providedIn: 'root' })
export class ToastService {

  constructor(private toastr: ToastrService) {}

  success(
    message: string,
    options?: Partial<IndividualConfig>
  ) {
    this.toastr.success(message, "Sucesso", options);
  }

  error(
    message: string,
    options?: Partial<IndividualConfig>
  ) {
    this.toastr.error(message, 'Erro', options);
  }

  info(
    message: string,
    options?: Partial<IndividualConfig>
  ) {
    this.toastr.info(message, "Info", options);
  }

  warning(
    message: string,
    options?: Partial<IndividualConfig>
  ) {
    this.toastr.warning(message, "Atenção", options);
  }
}
