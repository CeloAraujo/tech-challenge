import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { API_BASE } from '../nucleo/api';
import { PlanosLista } from './planos-lista';

describe('PlanosLista', () => {
  let fixture: ComponentFixture<PlanosLista>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlanosLista],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE, useValue: 'http://api.test' }
      ]
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PlanosLista);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('recarrega os planos ao clicar no botao e encerra o estado de carregamento', () => {
    http.expectOne('http://api.test/planos').flush([]);
    fixture.detectChanges();

    const botao: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(botao.disabled).toBeFalse();

    botao.click();
    fixture.detectChanges();

    expect(botao.disabled).toBeTrue();
    http.expectOne('http://api.test/planos').flush([
      { id: '1', nome: 'Plano Essencial', codigo_registro_ans: '123456789' }
    ]);
    fixture.detectChanges();

    expect(botao.disabled).toBeFalse();
    expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Plano Essencial');
  });

  it('apresenta o erro da API e libera nova tentativa quando a consulta falha', () => {
    http.expectOne('http://api.test/planos').flush(
      { mensagem: 'Falha ao consultar planos', detalhes: [] },
      { status: 500, statusText: 'Erro interno' },
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.erro').textContent).toContain(
      'Falha ao consultar planos',
    );
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBeFalse();
  });
});
