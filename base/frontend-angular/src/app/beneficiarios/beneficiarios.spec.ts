import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { of, Subject } from 'rxjs';
import { PlanoServico } from '../planos/plano-servico';
import { BeneficiarioServico } from './beneficiario-servico';
import { Beneficiarios, cpfValido, dataPassada, formatarCpf } from './beneficiarios';

describe('validadores de beneficiário', () => {
  it('aceita controle vazio ou nulo para que a obrigatoriedade seja tratada pelo required', () => {
    expect(cpfValido(new FormControl('', { nonNullable: true }))).toBeNull();
    expect(cpfValido(new FormControl(null) as unknown as FormControl<string>)).toBeNull();
  });

  it('aceita um CPF com dígitos verificadores válidos', () =>
    expect(cpfValido(new FormControl('52998224725', { nonNullable: true }))).toBeNull());

  it('trata resto dez como zero no cálculo oficial do dígito verificador', () =>
    expect(cpfValido(new FormControl('10000000108', { nonNullable: true }))).toBeNull());
  it('diferencia os tipos de CPF inválido', () => {
    expect(cpfValido(new FormControl('5299822472', { nonNullable: true }))).toEqual({
      cpfTamanho: true,
    });
    expect(cpfValido(new FormControl('5299822472A', { nonNullable: true }))).toEqual({
      cpfSomenteDigitos: true,
    });
    expect(cpfValido(new FormControl('11111111111', { nonNullable: true }))).toEqual({
      cpfRepetido: true,
    });
    expect(cpfValido(new FormControl('52998224724', { nonNullable: true }))).toEqual({
      cpfDigitosVerificadores: true,
    });
  });
  it('aceita somente uma data existente anterior a hoje', () => {
    expect(dataPassada(new FormControl('1990-05-12', { nonNullable: true }))).toBeNull();
    expect(dataPassada(new FormControl('2990-05-12', { nonNullable: true }))).toEqual({
      dataPassada: true,
    });
    expect(dataPassada(new FormControl('2024-02-30', { nonNullable: true }))).toEqual({
      dataPassada: true,
    });
  });
  it('formata CPF somente para exibicao, preservando onze digitos como valor de dominio', () => {
    expect(formatarCpf('52998224725')).toBe('529.982.247-25');
    expect(formatarCpf('529a982.247-25texto')).toBe('529.982.247-25');
  });
});

describe('paginação de beneficiários', () => {
  let fixture: ComponentFixture<Beneficiarios>;
  let listar: jasmine.Spy;

  beforeEach(async () => {
    listar = jasmine.createSpy().and.callFake((filtros: { pagina: number; tamanho: number }) =>
      of({ dados: [], pagina: filtros.pagina, tamanho: filtros.tamanho, total: 30 }),
    );
    await TestBed.configureTestingModule({
      imports: [Beneficiarios],
      providers: [
        { provide: BeneficiarioServico, useValue: { listar } },
        { provide: PlanoServico, useValue: { listar: () => of([]) } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(Beneficiarios);
    fixture.detectChanges();
  });

  it('reinicia na primeira página e consulta o servidor ao mudar o tamanho', () => {
    (fixture.componentInstance as unknown as { pagina: { set(valor: number): void } }).pagina.set(3);
    fixture.detectChanges();

    const seletor = fixture.nativeElement.querySelector(
      'select[aria-label="Registros por página"]',
    ) as HTMLSelectElement;
    seletor.value = '20';
    seletor.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(listar).toHaveBeenCalledWith(
      jasmine.objectContaining({ pagina: 1, tamanho: 20 }),
    );
    expect(fixture.nativeElement.textContent).toContain('página 1 de 2');
  });

  it('bloqueia a escolha de tamanho enquanto a lista está carregando', () => {
    listar.and.returnValue(new Subject());
    (fixture.componentInstance as unknown as { carregar(): void }).carregar();
    fixture.detectChanges();

    const seletor = fixture.nativeElement.querySelector(
      'select[aria-label="Registros por página"]',
    ) as HTMLSelectElement;
    expect(seletor.disabled).toBeTrue();
  });
});
