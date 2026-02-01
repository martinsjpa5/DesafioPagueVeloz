export class ApiErrorHelper {

  static getApiErrorMessage(err: any, fallbackMessage = 'Ocorreu um Erro no Sistema, contate o TI.'): string {
    if (err?.error?.erros?.length) {
      return err.error.erros.join('\n');
    }

    return fallbackMessage;
  }

}
