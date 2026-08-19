import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { PlanoServico } from '../planos/plano-servico';
import { Beneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';
import { Beneficiarios } from './beneficiarios';

describe('fluxos de Beneficiários', () => {
  const beneficiario: Beneficiario = {
    id: 'b-1', nome_completo: 'Maria Silva', cpf: '52998224725', data_nascimento: '1990-01-01',
    plano_id: 'p-1', status: 'ATIVO', data_cadastro: '2026-01-01',
  };
  let fixture: ComponentFixture<Beneficiarios>;
  let componente: any;
  let servico: jasmine.SpyObj<BeneficiarioServico>;
  let planoServico: jasmine.SpyObj<PlanoServico>;

  beforeEach(async () => {
    servico = jasmine.createSpyObj('BeneficiarioServico', ['listar', 'criar', 'atualizar', 'excluir']);
    planoServico = jasmine.createSpyObj('PlanoServico', ['listar']);
    servico.listar.and.returnValue(of({ dados: [beneficiario], pagina: 1, tamanho: 10, total: 1 }));
    planoServico.listar.and.returnValue(of([{ id: 'p-1', nome: 'Plano Ouro', codigo_registro_ans: '123456', descricao: '', ativo: true }]));
    await TestBed.configureTestingModule({
      imports: [Beneficiarios],
      providers: [
        { provide: BeneficiarioServico, useValue: servico },
        { provide: PlanoServico, useValue: planoServico },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(Beneficiarios);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  function preencherFormulario(): void {
    componente.formulario.setValue({ nome_completo: '  Maria Silva  ', cpf: '52998224725',
      data_nascimento: '1990-01-01', plano_id: 'p-1', status: 'ATIVO' });
  }

  it('carrega planos e lista e resolve o nome do plano', () => {
    expect(planoServico.listar).toHaveBeenCalled();
    expect(servico.listar).toHaveBeenCalledWith({ pagina: 1, tamanho: 10, status: undefined, plano_id: undefined });
    expect(componente.nomePlano('p-1')).toBe('Plano Ouro');
    expect(componente.nomePlano('inexistente')).toContain('indispon');
    expect(fixture.nativeElement.textContent).toContain('Maria Silva');
  });

  it('aplica filtros e navega apenas para páginas válidas', () => {
    componente.total.set(35); componente.pagina.set(2);
    componente.filtros.setValue({ status: 'INATIVO', plano_id: 'p-1' });
    componente.aplicarFiltros();
    expect(servico.listar).toHaveBeenCalledWith(jasmine.objectContaining({ pagina: 1, status: 'INATIVO', plano_id: 'p-1' }));
    componente.total.set(35);
    const chamadas = servico.listar.calls.count();
    componente.mudarPagina(2);
    componente.mudarPagina(0); componente.mudarPagina(9); componente.mudarPagina(2);
    expect(servico.listar.calls.count()).toBe(chamadas + 1);
  });

  it('ignora o mesmo tamanho e mudanças durante carregamento', () => {
    const chamadas = servico.listar.calls.count();
    componente.mudarTamanho({ target: { value: '10' } } as unknown as Event);
    componente.carregando.set(true);
    componente.mudarTamanho({ target: { value: '20' } } as unknown as Event);
    expect(servico.listar.calls.count()).toBe(chamadas);
  });

  it('cadastra com CPF sem máscara, nome aparado e recarrega', () => {
    servico.criar.and.returnValue(of(beneficiario)); preencherFormulario(); componente.salvar();
    expect(servico.criar).toHaveBeenCalledWith({ nome_completo: 'Maria Silva', cpf: '52998224725', data_nascimento: '1990-01-01', plano_id: 'p-1' });
    expect(componente.mensagemFormulario()).toContain('cadastrado');
    expect(componente.formulario.controls.cpf.value).toBe('');
    expect(servico.listar).toHaveBeenCalledTimes(2);
  });

  it('não envia formulário inválido nem permite envio simultâneo', () => {
    componente.salvar(); expect(servico.criar).not.toHaveBeenCalled();
    preencherFormulario(); componente.salvando.set(true); componente.salvar();
    expect(servico.criar).not.toHaveBeenCalled();
  });

  it('edita sem enviar CPF e restaura o formulário ao concluir', () => {
    servico.atualizar.and.returnValue(of(beneficiario)); componente.editar(beneficiario);
    expect(componente.formulario.controls.cpf.disabled).toBeTrue();
    componente.formulario.controls.nome_completo.setValue('Maria Atualizada'); componente.salvar();
    expect(servico.atualizar).toHaveBeenCalledWith('b-1', { nome_completo: 'Maria Atualizada', data_nascimento: '1990-01-01', plano_id: 'p-1', status: 'ATIVO' });
    expect(componente.mensagemFormulario()).toContain('atualizado');
    expect(componente.editandoId()).toBeNull();
    expect(componente.formulario.controls.cpf.enabled).toBeTrue();
  });

  it('normaliza a digitação do CPF mantendo apenas o valor de domínio', () => {
    const input = document.createElement('input'); input.value = '529a982.247-25 texto 99';
    componente.atualizarCpf({ target: input } as unknown as Event);
    expect(componente.formulario.controls.cpf.value).toBe('52998224725');
    expect(input.value).toBe('529.982.247-25');
  });

  it('mapeia erros estruturados para campos sem alerta genérico', () => {
    servico.criar.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400, error: {
      mensagem: 'Inválido', detalhes: [{ campo: 'cpf', regra: 'duplicado' }, { campo: 'desconhecido', regra: 'x' }, { campo: 'plano_id', regra: '' }],
    } })));
    preencherFormulario(); componente.salvar();
    expect(componente.formulario.controls.cpf.getError('api')).toBe('duplicado');
    expect(componente.erroFormulario()).toBeNull();
    expect(componente.salvando()).toBeFalse();
  });

  it('mostra erro geral quando não há erro de campo aplicável', () => {
    servico.criar.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { mensagem: 'Conflito' } })));
    preencherFormulario(); componente.salvar();
    expect(componente.erroFormulario()).toBe('Conflito');
  });

  it('só exclui após confirmação e recua da última linha da página', () => {
    spyOn(window, 'confirm').and.returnValues(false, true); servico.excluir.and.returnValue(of(void 0));
    componente.excluir(beneficiario); expect(servico.excluir).not.toHaveBeenCalled();
    componente.pagina.set(2); componente.excluir(beneficiario);
    expect(servico.excluir).toHaveBeenCalledWith('b-1');
    expect(componente.pagina()).toBe(1); expect(componente.mensagemLista()).toContain('exclu');
  });

  it('preserva a lista e apresenta erro quando a exclusão falha', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    servico.excluir.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404, error: { mensagem: 'Não encontrado' } })));
    componente.excluir(beneficiario);
    expect(componente.beneficiarios()).toEqual([beneficiario]);
    expect(componente.erroLista()).toBe('Não encontrado'); expect(componente.excluindoId()).toBeNull();
  });

  it('exibe estados de carregamento, vazio e falha da listagem', () => {
    componente.carregando.set(true); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Carregando');
    componente.carregando.set(false); componente.beneficiarios.set([]); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nenhum benefici');
    servico.listar.and.returnValue(throwError(() => new HttpErrorResponse({ status: 0 }))); componente.carregar();
    expect(componente.erroLista()).toContain('API'); expect(componente.carregando()).toBeFalse();
  });

  it('informa falha no carregamento de planos', () => {
    fixture.destroy();
    const resposta = new Subject<never>();
    planoServico.listar.and.returnValue(resposta);
    const outra = TestBed.createComponent(Beneficiarios); outra.detectChanges();
    resposta.error(new HttpErrorResponse({ status: 500, error: { mensagem: 'Planos indisponíveis' } }));
    expect((outra.componentInstance as any).erroLista()).toBe('Planos indisponíveis'); outra.destroy();
  });

  it('exibe erro de controle apenas depois da interação', () => {
    const controle = componente.formulario.controls.nome_completo;
    expect(componente.mostrarErro(controle)).toBeFalse(); controle.markAsTouched();
    expect(componente.mostrarErro(controle)).toBeTrue();
  });
});
