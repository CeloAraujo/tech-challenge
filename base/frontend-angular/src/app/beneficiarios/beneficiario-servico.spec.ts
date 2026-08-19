import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE } from '../nucleo/api';
import { BeneficiarioServico } from './beneficiario-servico';

describe('BeneficiarioServico', () => {
  let servico: BeneficiarioServico;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        BeneficiarioServico,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE, useValue: 'http://api.test' },
      ],
    });
    servico = TestBed.inject(BeneficiarioServico);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('monta paginação e filtros opcionais na listagem', () => {
    servico.listar({ pagina: 2, tamanho: 20, status: 'INATIVO', plano_id: 'plano-1' }).subscribe();
    const req = http.expectOne(
      (r) => r.url === 'http://api.test/beneficiarios' && r.params.get('pagina') === '2',
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('tamanho')).toBe('20');
    expect(req.request.params.get('status')).toBe('INATIVO');
    expect(req.request.params.get('plano_id')).toBe('plano-1');
    req.flush({ dados: [], pagina: 2, tamanho: 20, total: 0 });
  });

  it('não envia filtros vazios na listagem', () => {
    servico.listar({ pagina: 1, tamanho: 10 }).subscribe();
    const req = http.expectOne('http://api.test/beneficiarios?pagina=1&tamanho=10');
    expect(req.request.params.has('status')).toBeFalse();
    expect(req.request.params.has('plano_id')).toBeFalse();
    req.flush({ dados: [], pagina: 1, tamanho: 10, total: 0 });
  });

  it('envia o contrato de criação sem campos controlados pelo servidor', () => {
    const dados = { nome_completo: 'Maria Silva', cpf: '52998224725', data_nascimento: '1990-01-01', plano_id: 'plano-1' };
    servico.criar(dados).subscribe();
    const req = http.expectOne('http://api.test/beneficiarios');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dados);
    req.flush({ ...dados, id: 'b-1', status: 'ATIVO', data_cadastro: '2026-01-01' });
  });

  it('envia no PUT somente os campos editáveis, sem CPF', () => {
    const dados = { nome_completo: 'Maria Souza', data_nascimento: '1990-01-01', plano_id: 'plano-2', status: 'INATIVO' as const };
    servico.atualizar('b-1', dados).subscribe();
    const req = http.expectOne('http://api.test/beneficiarios/b-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dados);
    expect(req.request.body.cpf).toBeUndefined();
    req.flush({ ...dados, id: 'b-1', cpf: '52998224725', data_cadastro: '2026-01-01' });
  });

  it('exclui pelo identificador', () => {
    servico.excluir('b-1').subscribe();
    const req = http.expectOne('http://api.test/beneficiarios/b-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
