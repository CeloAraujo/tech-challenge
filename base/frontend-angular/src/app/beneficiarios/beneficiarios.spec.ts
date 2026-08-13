import { FormControl } from '@angular/forms';
import { cpfValido, dataPassada, formatarCpf } from './beneficiarios';

describe('validadores de beneficiário', () => {
  it('aceita um CPF com dígitos verificadores válidos', () =>
    expect(cpfValido(new FormControl('52998224725', { nonNullable: true }))).toBeNull());
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
