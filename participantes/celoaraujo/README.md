# Entrega — celoaraujo

## 1. Resumo

Completei o módulo de Beneficiários na API e na interface Angular, mantendo o desenho simples do módulo de Planos. No backend, substituí o acesso direto ao `AppDbContext` pelo `BeneficiarioServico`, criei contratos próprios de entrada e saída, implementei CRUD, validações, filtros combináveis, paginação, exclusão lógica e tratamento de concorrência. A entidade `Beneficiario` agora concentra invariantes de nome, CPF, nascimento, status e exclusão sem depender de ASP.NET Core ou EF Core.

No banco, acrescentei `ExcluidoEm`, filtro global e índice único de CPF. O índice não é filtrado por exclusão: um CPF excluído continua reservado e duas criações simultâneas não conseguem persistir duplicatas. A listagem usa `CountAsync` e uma consulta paginada com ordenação por `DataCadastro` e `Id`; a quantidade de consultas não cresce com o tamanho da página.

No frontend, criei modelos tipados, `BeneficiarioServico` com `HttpClient` e um componente standalone para listar, filtrar, paginar, cadastrar, editar e excluir. O formulário valida CPF e nascimento antes do envio, bloqueia CPF na edição, exibe erros estruturados da API e trata carregamento e lista vazia. A exclusão somente atualiza a tela depois da resposta de sucesso.

A navegação agora permite escolher 10, 20, 50 ou 100 registros por página. A troca do tamanho retorna à primeira página e faz uma nova consulta ao servidor, evitando manter uma página que deixou de existir com o novo tamanho.

Os contratos de criação e atualização foram agrupados em `Aplicacao/Contratos` e agora são enviados inteiros do controller para `BeneficiarioServico`, mantendo coesa a entrada de cada caso de uso sem acoplar a aplicação à pasta HTTP. As validações de CPF distinguem campo obrigatório, quantidade de dígitos, caracteres não numéricos, sequência repetida e dígitos verificadores. O frontend apresenta cada erro abaixo do campo correspondente; erros sem campo permanecem no alerta geral do formulário.

O campo CPF aplica máscara `000.000.000-00` somente na apresentação. O evento de entrada remove qualquer caractere não numérico, limita a 11 dígitos e mantém o `FormControl` com o valor cru; portanto POST envia exatamente os 11 dígitos exigidos pela API.

### Testes escritos

- `Criar_com_cpf_invalido_deve_devolver_400`: cobre CPF repetido, dígitos inválidos e CPF com máscara.
- O teste parametrizado de CPF também verifica `campo` e a regra específica devolvida para cada causa de invalidez.
- `Criar_deve_ignorar_campos_controlados_pelo_servidor`: prova que `id`, `status` e `data_cadastro` do cliente não controlam o registro.
- `Listar_com_paginacao_invalida_deve_devolver_400`: cobre `pagina=0` e `tamanho=101`.
- `Atualizar_status_de_beneficiario_inativo_deve_reativar`: cobre a transição permitida de `INATIVO` para `ATIVO`.
- `Criacoes_concorrentes_com_mesmo_cpf_devem_criar_apenas_um_registro`: espera um `201` e um `409` em duas requisições simultâneas.
- `beneficiarios.spec.ts`: três testes Jasmine para CPF válido, CPFs inválidos e datas inexistentes/futuras.
- `planos-lista.spec.ts`: reproduz o clique em “Recarregar”, confirma a segunda chamada HTTP e garante que o carregamento termina com a tabela atualizada.
- `Reativar_beneficiario_deve_manter_vinculo_com_plano_excluido`: cobre a reativação sem trocar o vínculo histórico depois da exclusão lógica do plano.
- `Atualizar_sem_status_deve_devolver_400_com_regra_obrigatorio`: garante que omitir `status` no PUT não use `ATIVO` implicitamente como valor padrão do enum.
- Os testes de precedência confirmam que dados cadastrais inválidos retornam `400` antes de consultar um plano inexistente (`422`).
- `Plano_excluido_deve_ser_recusado_na_criacao_e_na_atualizacao`: confirma `422` no POST e no PUT, preservando separadamente a reativação com vínculo histórico.
- A listagem ganhou cobertura para página além do total, paginação não numérica e filtros de situação e plano aplicados isoladamente.
- `HealthControllerTests`: confirma `503` com banco indisponível usando um `AppDbContext` isolado, sem interromper o PostgreSQL compartilhado pela suíte.
- Os testes Angular confirmam que mudar o tamanho volta à página 1, consulta o servidor e mantém o seletor bloqueado durante carregamento.

### Ampliação de cobertura com testes de comportamento

- `TratamentoDeErroMiddlewareTests` verifica preservação de erros de domínio, resposta `500` sanitizada sem informação interna, continuidade do pipeline sem erro e proteção quando a resposta HTTP já foi iniciada.
- A concorrência de Planos dispara dez criações simultâneas com o mesmo código e confirma uma única persistência: uma resposta `201` e nove conflitos `409`.
- Os testes adicionais de domínio cobrem plano obrigatório, `status` inválido e campos obrigatórios de Plano, verificando códigos e detalhes do contrato.
- `beneficiario-servico.spec.ts` valida URLs, parâmetros opcionais, POST, PUT sem CPF e DELETE por meio de `HttpTestingController`.
- `beneficiarios-fluxos.spec.ts` cobre cadastro, edição, cancelamento, filtros, paginação, máscara, bloqueios, loading, lista vazia, exclusão cancelada, sucesso/falha e mensagens por campo ou gerais.
- `api.spec.ts` cobre falha de conexão, fallback por status e tradução do contrato estruturado de erro.
- Os testes de Planos passaram a cobrir o serviço HTTP e os estados de sucesso, falha e nova tentativa da listagem.
- O cálculo do CPF possui caso explícito para a regra oficial em que resto dez deve produzir dígito zero; controles vazios/nulos permanecem sob responsabilidade do `required`.

A cobertura final do frontend ficou em 100% das linhas e funções, 99,45% dos statements e 98,3% dos branches. No backend, o código principal ficou com 97,84% das linhas cobertas; o relatório bruto é menor porque também contabiliza migrations, arquivos gerados, `Program.cs` e a factory de design-time do EF Core. Não foram criados testes artificiais para executar getters triviais ou métodos `Down` apenas para elevar a métrica.

### Testes modificados

- Renomeei e alterei `Listar_sem_informar_tamanho_deve_devolver_20_itens_por_pagina` para esperar 10. O teste original contradizia a seção 3 da `SPEC.md`, que define tamanho padrão 10.
- Alterei `Atualizar_dados_de_beneficiario_inativo_deve_devolver_200` para esperar `409 Conflict`. A seção 2.3 da `SPEC.md` define o beneficiário inativo como congelado.
- Nenhum teste foi removido ou enfraquecido; os demais testes públicos foram preservados.

O build Angular e os 34 testes frontend passaram. A suíte backend foi executada com o SDK .NET 10 e passou com 52 de 52 testes. O `docker compose up -d --build` também passou; validei health/banco, cinco planos, criação e listagem de beneficiário, Swagger e web. As restaurações reportaram vulnerabilidades em dependências transitivas (`SSH.NET` no teste e pacotes NPM); não apliquei atualização automática sem análise de compatibilidade.

## 2. Decisões

### Defeitos encontrados e correções

O controller original de Beneficiários misturava HTTP, acesso ao banco e regra de negócio. Além de violar a separação já usada por Planos, ele fazia consultas síncronas dentro de método assíncrono, retornava formatos/códigos incorretos e expunha a entidade como corpo do POST. Criei DTOs de entrada em `Aplicacao/Contratos/BeneficiarioContratos.cs`, mantive responses em `Api/Contratos/BeneficiarioContratos.cs`, movi os casos de uso para `Aplicacao/BeneficiarioServico.cs` e deixei `BeneficiariosController.cs` responsável apenas pela tradução HTTP.

O código base de Planos também tinha um defeito em `planos/planos-lista.ts`. `carregar()` usava `takeUntilDestroyed()` sem informar `DestroyRef`. A carga inicial funcionava por ser chamada no construtor, mas o clique em “Recarregar” executava fora do contexto de injeção, lançava `NG0203` depois de marcar `carregando=true` e deixava a tela presa em “Carregando planos...”. Injetei `DestroyRef` no componente e passei `this.destroyRef` ao operador. `planos-lista.spec.ts` cobre especificamente o clique e evita regressão.

A validação original aceitava CPF apenas pelo tamanho, não impedia sequências repetidas e podia falhar com valor nulo. `Dominio/Beneficiario.cs` agora valida os 11 caracteres literais, formato numérico, repetição e os dois dígitos verificadores. Nome e nascimento também são validados na entidade. Mantive essas invariantes juntas para evitar duplicação entre criação e atualização. A serialização de enums não aceita inteiros, restringindo status a `ATIVO` e `INATIVO`.

A checagem de CPF antes do insert não garantia concorrência: duas requisições podiam observar ausência e inserir juntas. `AppDbContext.cs` agora possui índice único, e `BeneficiarioServico.SalvarAsync` traduz a violação PostgreSQL `23505` para o mesmo conflito `409` usado na checagem amigável. Assim, a pré-checagem melhora a mensagem, mas o banco é a garantia definitiva.

A listagem original não tinha envelope, filtros ou paginação e podia executar uma consulta adicional por plano. A nova consulta retorna apenas beneficiários, pois o contrato contém `plano_id`; o frontend carrega Planos uma vez e resolve os nomes em memória. A API executa uma contagem e uma consulta de página, sem N+1.

### Pontos omissos ou contraditórios

A especificação exige apenas que `data_nascimento` seja uma data anterior ao dia atual. Não estabeleci idade mínima para o beneficiário porque essa regra não foi solicitada; adicioná-la criaria uma regra de negócio nova e poderia rejeitar cadastros que estão válidos segundo a `SPEC.md`.

A especificação não define a ordem padrão. Escolhi `DataCadastro` crescente com `Id` como desempate. Isso mantém uma ordem temporal intuitiva e total, necessária para paginação estável quando cadastros compartilham o mesmo instante.

Beneficiário já vinculado a plano posteriormente excluído pode ser reativado se nome, nascimento e `plano_id` permanecerem idênticos. Essa operação só muda status e preserva vínculo histórico, portanto não exige que o plano volte a ser selecionável. Criação e atualização que estabeleçam ou troquem vínculo para plano excluído continuam retornando `422`.

Para um beneficiário inativo, interpretei “a mudança de status continua permitida” como reativação mantendo os dados cadastrais atuais. Se a mesma requisição tentar reativar e alterar nome, nascimento ou plano, retorna `409`. Essa interpretação evita contornar o congelamento juntando alterações à reativação.

No PUT, `status` é obrigatório. O DTO usa enum nullable para distinguir `ATIVO` do campo ausente: sem essa distinção, o model binding atribuiria zero, que corresponde a `ATIVO`, e uma omissão poderia reativar o beneficiário sem intenção. Campo ausente ou `null` retorna `400` com detalhe `status/obrigatorio`; texto de enum desconhecido continua retornando `400` como inválido.

A especificação não define qual erro prevalece quando a mesma requisição contém dados cadastrais inválidos e um plano inexistente. Escolhi validar primeiro os dados em memória (`400`), depois a referência ao plano (`422`) e, por fim, duplicidade ou congelamento (`409`). Isso evita consulta ao banco para um corpo já inválido e torna a resposta mais previsível. Na atualização, a validação foi separada da mutação para que uma falha não altere a entidade rastreada antes de verificar o plano.

A seção 9 não exige campo de consulta por ID na interface, embora a API exponha `GET /beneficiarios/{id}`. Mantive esse endpoint no backend para cumprir o contrato, mas não expus uma busca por UUID que o usuário não tem como conhecer. A tela oferece somente os filtros exigidos: situação e plano. Também separei mensagens de formulário das mensagens da listagem, desabilitei o botão da linha atualmente em edição e removi a recarga manual redundante; CRUD, filtros e paginação já atualizam a lista pelo servidor.

A seção 9.3 pede navegação usando `pagina` e `tamanho`. Inicialmente a tela sempre enviava o tamanho padrão 10; acrescentei opções 10, 20, 50 e 100 para expor ao usuário a capacidade que a API já oferecia. Ao trocar a opção, a página volta para 1 e o filtro atual é preservado.

Os dois testes públicos contraditórios foram modificados porque `SPEC.md` é o contrato funcional: tamanho padrão 10 e alteração cadastral de inativo com `409`. As mudanças estão enumeradas na seção anterior para tornar a decisão auditável.

### Escolhas técnicas

Usei SOLID sem criar interfaces sem consumidor. Controller, serviço, domínio e infraestrutura têm responsabilidades distintas, mas não criei repository genérico nem interfaces de serviço usadas por uma única implementação. O EF Core já fornece a abstração de persistência necessária neste escopo.

Mantive DRY colocando validações compartilhadas na entidade e o parser de erro frontend em `nucleo/api.ts`. Mantive KISS com estado local e Reactive Forms; não adicionei NgRx, biblioteca visual, AutoMapper ou pacote de validação. A solução fica curta, alinhada ao código base e explicável durante a entrevista.

## 3. Uso de IA

**Nível de uso:** apoio com agentes de IA durante análise, implementação e revisão.

Mantive como critérios explícitos a `SPEC.md`, o módulo de Planos, SOLID pragmático, DRY e KISS. Todas as alterações foram revisadas e validadas por build, testes automatizados e execução em Docker.

### Ferramentas

- IA generativa para leitura crítica da especificação, revisão de arquitetura, implementação assistida e sugestões de testes.
- `dotnet test`, Angular CLI e Karma/ChromeHeadless para validar backend e frontend.
- Docker Compose e requisições HTTP para validar a integração com PostgreSQL e a aplicação executável.

### Os 3 prompts que mais influenciaram o resultado

**Prompt 1**

> Leia integralmente o `README.md` e a `SPEC.md` da raiz antes de alterar o projeto. Analise o backend .NET, o frontend Angular, os testes existentes e o ambiente Docker dentro de `base/`. Implemente o desafio seguindo a especificação como fonte de verdade. Aplique SOLID de forma pragmática, sem criar interfaces ou abstrações sem necessidade, além de DRY e KISS. Preserve o módulo de Planos e seu contrato público, exceto quando houver um defeito comprovado. Corrija os defeitos encontrados, implemente os requisitos ausentes e escreva ou ajuste testes somente quando houver justificativa técnica. Ao final, execute testes, builds e valide a integração da aplicação.

- **O que aceitei:** separação entre controller, serviço, domínio e infraestrutura; DTOs próprios; validações de domínio; testes de integração; paginação e tratamento centralizado de erros.
- **O que descartei e por quê:** repository genérico, AutoMapper e interfaces com uma única implementação, pois aumentariam a quantidade de código sem resolver uma necessidade deste escopo.

**Prompt 2**

> Refatore o fluxo de criação e atualização para que o controller envie o DTO completo ao serviço, mantendo a entrada do caso de uso coesa e sem passar cada propriedade como argumento separado. Preserve o contrato JSON em `snake_case` e evite acoplar a camada de aplicação aos contratos de resposta HTTP. Na validação do CPF, use verificações explícitas e ordenadas para diferenciar: campo obrigatório, quantidade diferente de 11 caracteres, presença de caracteres não numéricos, sequência de dígitos repetidos e dígitos verificadores incorretos. Cada falha deve retornar campo, regra e mensagem específicos. Mantenha o conflito de CPF duplicado como `409` e preserve a constraint única no PostgreSQL como garantia contra concorrência.

- **O que aceitei:** DTO completo chegando ao serviço, contratos de entrada na camada de aplicação, validação em ordem segura e regras distintas para cada falha de CPF.
- **O que descartei e por quê:** criação de mapper, interface de serviço ou classe de validação para cada regra; a entidade e uma função auxiliar pequena mantêm as invariantes com menor complexidade.

**Prompt 3**

> Revise a experiência do módulo de Beneficiários no Angular. Exiba validações de nome, CPF, data de nascimento e plano abaixo do respectivo campo, reservando espaço para as mensagens para que o formulário não fique desalinhado. Mapeie os detalhes estruturados retornados pela API para o `FormControl` correspondente e mantenha um alerta geral apenas para erros sem campo. Aplique máscara visual ao CPF, aceite somente números e envie ao backend apenas os 11 dígitos. Separe mensagens do formulário das mensagens da listagem, desabilite o botão da linha que já está em edição, impeça duplo envio e atualize a lista somente após operações concluídas com sucesso. Preserve filtros e paginação no servidor e escreva testes para os comportamentos críticos.

- **O que aceitei:** erros por campo, espaço fixo para mensagens, máscara apenas visual, separação dos avisos, bloqueio de ações concorrentes e atualização após sucesso.
- **O que descartei e por quê:** biblioteca de máscara e gerenciamento global de estado, pois o evento de entrada, Reactive Forms e signals locais atendem ao requisito sem dependências adicionais.

### O que fiz sem IA

Sem IA, revisei o código gerado e fiz verificações manuais dos principais fluxos da aplicação. A partir dessa revisão, defini e corrigi pontos que considerei importantes, como o envio dos DTOs completos entre controller e service, ajustes pontuais nos eventos e estados da interface, organização das mensagens de validação e bloqueio de ações durante edição e exclusão. Também revisei os contratos HTTP, a documentação Swagger e decisões de UX, validando as alterações por meio de testes, build e execução em Docker.

### O que ainda não domino completamente

Consigo explicar os arquivos entregues e as decisões acima. A geração mecânica do arquivo Designer e do snapshot de migrations do EF Core é a parte em que ainda consultaria a documentação para alterações mais complexas, embora eu compreenda o efeito desta migration no schema.

## 4. Perguntas de compreensão

### 1. Concorrência

Duas requisições podem passar quase juntas pela consulta `AnyAsync` em `BeneficiarioServico.CriarAsync`.
Por isso essa consulta não é tratada como garantia de unicidade; ela só antecipa um erro amigável.
A garantia real está no índice único de `Cpf`, configurado em `Infraestrutura/AppDbContext.cs`.
Esse índice também aparece na migration `20260813120000_CompletarBeneficiarios.cs`.
O índice não possui filtro por `ExcluidoEm`, então CPF de registro excluído continua ocupado.
Quando as duas transações tentam salvar, PostgreSQL aceita uma e rejeita a outra com SQLSTATE `23505`.
`BeneficiarioServico.SalvarAsync` captura essa `PostgresException` dentro de `DbUpdateException`.
Ela é convertida em `ConflitoException`, e o middleware responde `409` no formato padrão.
O teste `Criacoes_concorrentes_com_mesmo_cpf_devem_criar_apenas_um_registro` confirma um `201` e um `409`.

### 2. Um defeito que corrigi

O POST original recebia a própria entidade `Beneficiario`, permitindo mass assignment.
Um cliente podia enviar `Id`, `Status` e `DataCadastro`, embora a especificação atribua esses campos ao servidor.
Em produção, isso permitiria criar beneficiário já inativo, falsificar o horário do cadastro ou escolher identificador.
A correção está em `Aplicacao/Contratos/BeneficiarioContratos.cs`: `BeneficiarioCriacaoRequest` só expõe quatro campos.
O controller passa esses valores ao construtor de domínio, que gera `Guid.NewGuid()` e `DateTime.UtcNow`.
O construtor também fixa `StatusBeneficiario.ATIVO`.
Campos JSON extras são ignorados pelo desserializador e nunca chegam ao caso de uso.
O teste `Criar_deve_ignorar_campos_controlados_pelo_servidor` envia valores falsos e confirma os valores do servidor.

### 3. O trecho mais complexo

O trecho mais delicado é `Beneficiario.ValidarCpf`, embora permaneça pequeno.
Primeiro ele normaliza nulo para string vazia, sem remover espaços, pois o contrato exige 11 dígitos exatos.
Em seguida, a função exige exatamente 11 caracteres e depois confirma que todos são dígitos ASCII.
`Distinct().Count() == 1` rejeita sequências repetidas como `11111111111`.
Para o primeiro verificador, `CalcularDigito(cpf[..9], 10)` aplica pesos de 10 até 2.
A soma é multiplicada por 10, reduzida módulo 11 e o resultado 10 vira zero.
O valor calculado é comparado ao décimo caractere convertido para número.
O segundo cálculo usa os dez primeiros dígitos e pesos de 11 até 2.
Cada falha gera `ValidacaoException` com uma regra específica; o sucesso retorna o CPF validado.
