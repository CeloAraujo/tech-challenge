import { HttpErrorResponse } from '@angular/common/http';
import { mensagemDeErro } from './api';

describe('mensagemDeErro', () => {
  it('explica falha de conexão', () => {
    expect(mensagemDeErro(new HttpErrorResponse({ status: 0 }))).toContain('Confira se ela está no ar');
  });

  it('usa status quando a API não devolve contrato estruturado', () => {
    expect(mensagemDeErro(new HttpErrorResponse({ status: 502, error: {} }))).toBe('A API respondeu 502.');
  });

  it('apresenta mensagem e detalhes estruturados', () => {
    const resposta = new HttpErrorResponse({
      status: 400,
      error: { mensagem: 'Dados inválidos', detalhes: [{ campo: 'cpf', regra: 'duplicado' }] },
    });
    expect(mensagemDeErro(resposta)).toBe('Dados inválidos: cpf (duplicado)');
  });

  it('preserva a mensagem da API quando não há detalhes', () => {
    const resposta = new HttpErrorResponse({ status: 404, error: { mensagem: 'Não encontrado', detalhes: [] } });
    expect(mensagemDeErro(resposta)).toBe('Não encontrado');
  });
});
