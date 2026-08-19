import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE } from '../nucleo/api';
import { PlanoServico } from './plano-servico';

describe('PlanoServico', () => {
  it('consulta os planos na URL configurada', () => {
    TestBed.configureTestingModule({
      providers: [
        PlanoServico,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE, useValue: 'http://api.test' },
      ],
    });
    const servico = TestBed.inject(PlanoServico);
    const http = TestBed.inject(HttpTestingController);
    const resposta = [{ id: 'p-1', nome: 'Ouro', codigo_registro_ans: '123456' }];

    servico.listar().subscribe((planos) => expect(planos).toEqual(resposta));
    const req = http.expectOne('http://api.test/planos');
    expect(req.request.method).toBe('GET');
    req.flush(resposta);
    http.verify();
  });
});
