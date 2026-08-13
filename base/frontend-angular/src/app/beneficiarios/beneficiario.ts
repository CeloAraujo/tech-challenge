export type StatusBeneficiario = 'ATIVO' | 'INATIVO';

export interface Beneficiario {
  id: string;
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  status: StatusBeneficiario;
  plano_id: string;
  data_cadastro: string;
}

export interface PaginaBeneficiarios {
  dados: Beneficiario[];
  pagina: number;
  tamanho: number;
  total: number;
}
export interface FiltrosBeneficiarios {
  pagina: number;
  tamanho: number;
  status?: StatusBeneficiario;
  plano_id?: string;
}
export interface CriarBeneficiario {
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  plano_id: string;
}
export interface AtualizarBeneficiario {
  nome_completo: string;
  data_nascimento: string;
  plano_id: string;
  status: StatusBeneficiario;
}
