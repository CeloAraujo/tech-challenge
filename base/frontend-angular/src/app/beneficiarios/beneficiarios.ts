import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ErroDaApi, mensagemDeErro } from '../nucleo/api';
import { Plano } from '../planos/plano';
import { PlanoServico } from '../planos/plano-servico';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';

export function cpfValido(controle: AbstractControl<string>): ValidationErrors | null {
  const cpf = controle.value ?? '';
  if (!cpf) return null;
  if (cpf.length !== 11) return { cpfTamanho: true };
  if (!/^\d+$/.test(cpf)) return { cpfSomenteDigitos: true };
  if (/^(\d)\1{10}$/.test(cpf)) return { cpfRepetido: true };
  const digito = (quantidade: number): number => {
    let soma = 0;
    for (let i = 0; i < quantidade; i++) soma += Number(cpf[i]) * (quantidade + 1 - i);
    const resto = (soma * 10) % 11;
    return resto === 10 ? 0 : resto;
  };
  return digito(9) === Number(cpf[9]) && digito(10) === Number(cpf[10])
    ? null
    : { cpfDigitosVerificadores: true };
}

export function dataPassada(controle: AbstractControl<string>): ValidationErrors | null {
  if (!controle.value) return null;
  const partes = controle.value.split('-').map(Number);
  if (partes.length !== 3) return { dataPassada: true };
  const data = new Date(partes[0], partes[1] - 1, partes[2]);
  const hoje = new Date();
  hoje.setHours(0, 0, 0, 0);
  const existe =
    data.getFullYear() === partes[0] &&
    data.getMonth() === partes[1] - 1 &&
    data.getDate() === partes[2];
  return existe && data < hoje ? null : { dataPassada: true };
}

export function formatarCpf(cpf: string): string {
  const digitos = cpf.replace(/\D/g, '').slice(0, 11);
  return digitos
    .replace(/^(\d{3})(\d)/, '$1.$2')
    .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/(\d{3})(\d{1,2})$/, '$1-$2');
}

@Component({
  selector: 'app-beneficiarios',
  imports: [ReactiveFormsModule],
  templateUrl: './beneficiarios.html',
  styleUrl: './beneficiarios.css',
})
export class Beneficiarios {
  private readonly servico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly beneficiarios = signal<Beneficiario[]>([]);
  protected readonly planos = signal<Plano[]>([]);
  protected readonly carregando = signal(true);
  protected readonly salvando = signal(false);
  protected readonly excluindoId = signal<string | null>(null);
  protected readonly erroFormulario = signal<string | null>(null);
  protected readonly mensagemFormulario = signal<string | null>(null);
  protected readonly erroLista = signal<string | null>(null);
  protected readonly mensagemLista = signal<string | null>(null);
  protected readonly pagina = signal(1);
  protected readonly tamanho = signal(10);
  protected readonly total = signal(0);
  protected readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.tamanho())),
  );
  protected readonly editandoId = signal<string | null>(null);
  protected readonly formatarCpf = formatarCpf;

  protected readonly filtros = new FormGroup({
    status: new FormControl<StatusBeneficiario | ''>('', { nonNullable: true }),
    plano_id: new FormControl('', { nonNullable: true }),
  });
  protected readonly formulario = new FormGroup({
    nome_completo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(120)],
    }),
    cpf: new FormControl('', { nonNullable: true, validators: [Validators.required, cpfValido] }),
    data_nascimento: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, dataPassada],
    }),
    plano_id: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    status: new FormControl<StatusBeneficiario>('ATIVO', { nonNullable: true }),
  });

  constructor() {
    this.carregarPlanos();
    this.carregar();
  }

  protected aplicarFiltros(): void {
    this.pagina.set(1);
    this.carregar();
  }
  protected mudarPagina(valor: number): void {
    if (valor < 1 || valor > this.totalPaginas() || valor === this.pagina()) return;
    this.pagina.set(valor);
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroLista.set(null);
    const filtros = this.filtros.getRawValue();
    this.servico
      .listar({
        pagina: this.pagina(),
        tamanho: this.tamanho(),
        status: filtros.status || undefined,
        plano_id: filtros.plano_id || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.beneficiarios.set(r.dados);
          this.pagina.set(r.pagina);
          this.tamanho.set(r.tamanho);
          this.total.set(r.total);
          this.carregando.set(false);
        },
        error: (r: HttpErrorResponse) => {
          this.erroLista.set(mensagemDeErro(r));
          this.carregando.set(false);
        },
      });
  }

  protected salvar(): void {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid || this.salvando()) return;
    this.salvando.set(true);
    this.erroFormulario.set(null);
    this.mensagemFormulario.set(null);
    const dados = this.formulario.getRawValue();
    const id = this.editandoId();
    const requisicao = id
      ? this.servico.atualizar(id, {
          nome_completo: dados.nome_completo.trim(),
          data_nascimento: dados.data_nascimento,
          plano_id: dados.plano_id,
          status: dados.status,
        })
      : this.servico.criar({
          nome_completo: dados.nome_completo.trim(),
          cpf: dados.cpf,
          data_nascimento: dados.data_nascimento,
          plano_id: dados.plano_id,
        });
    requisicao.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.salvando.set(false);
        this.cancelarEdicao();
        this.mensagemFormulario.set(id ? 'Beneficiário atualizado.' : 'Beneficiário cadastrado.');
        this.carregar();
      },
      error: (r: HttpErrorResponse) => {
        const possuiErroDeCampo = this.aplicarErrosDaApi(r);
        if (!possuiErroDeCampo) this.erroFormulario.set(mensagemDeErro(r));
        this.salvando.set(false);
      },
    });
  }

  protected editar(b: Beneficiario): void {
    this.editandoId.set(b.id);
    this.formulario.setValue({
      nome_completo: b.nome_completo,
      cpf: b.cpf,
      data_nascimento: b.data_nascimento,
      plano_id: b.plano_id,
      status: b.status,
    });
    this.formulario.controls.cpf.disable();
    this.erroFormulario.set(null);
    this.mensagemFormulario.set(null);
  }
  protected atualizarCpf(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    const digitos = input.value.replace(/\D/g, '').slice(0, 11);
    this.formulario.controls.cpf.setValue(digitos);
    input.value = formatarCpf(digitos);
  }
  protected cancelarEdicao(): void {
    this.editandoId.set(null);
    this.formulario.reset({
      nome_completo: '',
      cpf: '',
      data_nascimento: '',
      plano_id: '',
      status: 'ATIVO',
    });
    this.formulario.controls.cpf.enable();
  }
  protected excluir(b: Beneficiario): void {
    if (!window.confirm(`Excluir ${b.nome_completo}?`)) return;
    this.excluindoId.set(b.id);
    this.erroLista.set(null);
    this.mensagemLista.set(null);
    this.servico
      .excluir(b.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.excluindoId.set(null);
          this.mensagemLista.set('Beneficiário excluído.');
          if (this.beneficiarios().length === 1 && this.pagina() > 1)
            this.pagina.update((v) => v - 1);
          this.carregar();
        },
        error: (r: HttpErrorResponse) => {
          this.erroLista.set(mensagemDeErro(r));
          this.excluindoId.set(null);
        },
      });
  }
  protected nomePlano(id: string): string {
    return this.planos().find((p) => p.id === id)?.nome ?? 'Plano indisponível';
  }
  protected mostrarErro(campo: AbstractControl): boolean {
    return campo.invalid && (campo.dirty || campo.touched);
  }

  private aplicarErrosDaApi(resposta: HttpErrorResponse): boolean {
    const corpo = resposta.error as Partial<ErroDaApi> | null;
    if (!Array.isArray(corpo?.detalhes)) return false;

    let aplicou = false;
    for (const detalhe of corpo.detalhes) {
      const controle = this.formulario.get(detalhe.campo);
      if (!controle || !detalhe.regra) continue;
      controle.setErrors({ ...(controle.errors ?? {}), api: detalhe.regra });
      controle.markAsTouched();
      aplicou = true;
    }
    return aplicou;
  }
  private carregarPlanos(): void {
    this.planoServico
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (planos) => this.planos.set(planos),
        error: (r: HttpErrorResponse) => this.erroLista.set(mensagemDeErro(r)),
      });
  }
}
