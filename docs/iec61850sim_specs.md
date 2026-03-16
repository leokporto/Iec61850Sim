# Especificação Técnica — Iec61850Sim

Repositório oficial:

[https://github.com/leokporto/Iec61850Sim](https://github.com/leokporto/Iec61850Sim)

**SEMPRE** referencie o repositório oficial ao criar código, a não ser que algum código seja compartilhado diretamente.

---

# 1. Visão Geral da Arquitetura e Stack

## Tecnologias

O projeto **Iec61850Sim** é um simulador IEC 61850 implementado como uma **self-hosted web application** utilizando tecnologias do ecossistema .NET.

A aplicação implementa:

- um servidor IEC 61850
- um simulador de dispositivos
- uma aplicação web para hospedagem e extensibilidade futura
- Uma aplicação Wpf com WebView2 para uso em windows

### Linguagem

- **C#**

### Frameworks

- **ASP.NET Core**
- **Blazor Server com interação**
- **.NET 10**


---

### Biblioteca IEC 61850

Biblioteca utilizada:

```
iec61850dotnet
```

Parte do projeto:

[https://github.com/mz-automation/libiec61850](https://github.com/mz-automation/libiec61850)

Documentação oficial:

- [https://github.com/mz-automation/libiec61850](https://github.com/mz-automation/libiec61850)
- [https://github.com/mz-automation/libiec61850/tree/v1.6/dotnet](https://github.com/mz-automation/libiec61850/tree/v1.6/dotnet)
- [https://github.com/mz-automation/libiec61850/tree/v1.6/tools/model_generator_dotnet](https://github.com/mz-automation/libiec61850/tree/v1.6/tools/model_generator_dotnet)
- [https://libiec61850.com/documentation/](https://libiec61850.com/documentation/)
- [https://libiec61850.com/documentation/using-the-c-api/](https://libiec61850.com/documentation/using-the-c-api/)
- [https://support.mz-automation.de/doc/libiec61850/net/latest/](https://support.mz-automation.de/doc/libiec61850/net/latest/)


---

### Requisitos de Build

Prerequisites:

- .NET 10 SDK
- Bibliotecas IEC 61850 da MZ-Automation
- Arquivo `.cfg`
- Arquivo `.icd`

---

### Plataformas suportadas

O simulador deve funcionar em:

- Windows
- Linux
- Docker containers

---

### Hosting

O projeto executa dentro de:

```
ASP.NET Core Web Host
```

e não deve ser tratado como uma aplicação console.

---

## Arquitetura

Arquitetura geral:

```
Self-Hosted Web Application
```

A arquitetura segue um **monorepo modular**.

Os módulos principais são:

```
Web Host
Wpf Client
Model Loader
Model Scanner
Point Registry
Device Builder
Device Manager
Simulation Engine
IEC Server Host
Simulation Background Service
```

Fluxo principal da aplicação:

```
1. ASP.NET Core Web Host é iniciado
2. O modelo IEC 61850 é carregado
3. Serviços são registrados no DI
4. O modelo é escaneado
5. Dispositivos são criados
6. Servidor IEC 61850 é iniciado
7. Um BackgroundService executa a simulação
8. Valores simulados são publicados periodicamente
```

---

### Loop de simulação

A simulação roda em um **Hosted Background Service**.

Fluxo:

```
simulation.Step()
iecServerHost.Publish()
```

Intervalo:

```
100–500 ms
```

---

### Objetivo do projeto

Construir um simulador capaz de:

- carregar modelos IEC
- simular medições
- simular estados de dispositivos
- permitir conexão de clientes SCADA
- publicar valores IEC 61850
- ser monitorado via web API

---

# 2. Estrutura de Diretórios e Convenções

## Estrutura do Repositório

Estrutura principal:

```
Iec61850Sim.slnx
README.md
ConfigFiles/
Docker/
src/
	Iec61850Sim.Core/
	Iec61850Sim.Web/
	Iec61850Sim.Desktop/
	Iec61850Sim.Tests/
ThirdPartyRefs/
    linux/
```

Descrição:

| Diretório           | Descrição                                       |
| ------------------- | ----------------------------------------------- |
| Iec61850Sim.Core    | funcionalidades principais                      |
| Iec61850Sim.Web     | Blazor Server application                       |
| Iec61850Sim.Desktop | aplicação WPF com WebView2                      |
| Iec61850Sim.Tests   | projeto de testes unitários da Iec61850Sim.Core |
| ConfigFiles         | arquivos IEC (CFG / SCL)                        |
| Docker              | dockerfiles                                     |
| ThirdPartyRefs      | bibliotecas libiec61850                         |

---

## ConfigFiles

Arquivos IEC armazenados em:

```
Iec61850Sim.Web/Config/
```

Arquivos padrão:

```
Demo_Ed2.cfg
Demo_Ed2.icd
rfc1006.cfg
```

---

## ThirdPartyRefs

Contém bibliotecas externas:

```
iec61850dotnet.dll
iec61850.dll (windows)
libiec61850.so (linux e docker)
libiec61850.so.1.6.1 (linux e docker)
```

Este diretório está no `.gitignore` por questões de licença.

---

## Convenções de Código

### Padrões de nomenclatura

Tudo, exceto os comentários deve ser escrito em inglês. Os comentários serão feitos em português (PT-BR). Palavras técnicas em inglês podem ser usadas nos comentários se fizerem mais sentido assim.

Seguir convenções padrão do C#: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names

Exceções:

- **Constantes** - Usar UpperCase com separação por underscore (`_`).
Exemplo: `MINHA_CONSTANTE`
- **Enums** - Usar `e` + PascalCase.
Exemplo: `eLnType`
- **Variáveis de classe** - usar o character `_`  + camelCase.
Exemplo: `_minhaVarDeClasse`


---

### Princípios de Arquitetura

O projeto deve seguir:

- SOLID
- Separation of Concerns
- Dependency Injection
- Baixo acoplamento

Utilizar Vertical Slices para separar domínios

Exemplo:
```
Simulation - para itens de simulação
Commands - para a parte de comandos
Device - para a parte de dispositivos como DeviceManager ou DeviceBuilder
```

---

### Regras específicas do libiec61850

A API .NET difere da API C.

Regras obrigatórias:

- Nunca assumir que APIs da versão C existem na versão .NET
- Sempre verificar documentação oficial
- Nunca inventar métodos inexistentes

---

### Referências IEC

Formato:

```
LogicalDevice/LogicalNode.DataObject.SubObject.Attribute
```

Exemplo:

```
DemoMeasurement/U3pMMXU2.PhV.phsA.cVal.mag
DemoProtCtrl/Obj1CSWI1.Pos.Oper.ctlVal
```

---

# 3. Configurações e Variáveis de Ambiente

## Configuração do modelo IEC

Arquivo padrão carregado:

```
Demo_Ed2.cfg
```

Local:

```
Iec61850Sim.Web/Config/
```

---

### Conversão de SCL

Arquivo SCL pode ser convertido para CFG usando:

```
java -jar getconfig.jar Demo_Ed2.scl Demo_Ed2.cfg
```

---

### IED Name

Nome padrão configurado:

```
Demo
```

Trecho do XML:

```xml
<IED name="Demo" type="S61850 for PC" manufacturer="INFO TECH" configVersion="1.0">
```

Atualmente o nome do IED é **hardcoded**.

Backlog existente:

```
extrair nome do IED da configuração
```

---

### Variáveis de ambiente

```
IEC_MODEL - refere-se ao arquivo de configuração (.cfg) a ser utilizado
```

---

### Dependências externas

Bibliotecas necessárias:

```
Windows:
iec61850.dll
iec61850dotnet.dll

Linux e Docker:
libiec61850.so
libiec61850.so.1.6.1
iec61850dotnet.dll
```

Fonte:

[https://github.com/mz-automation/libiec61850](https://github.com/mz-automation/libiec61850)

---

# 4. Definição do Domínio (Serviços, Modelos e Jobs)

## Modelos principais

### DevicePoint

Entidade que representa os pontos scanneados no modelo de configuração e que serão passados para o IedServer.
### PointRegistry

Responsável por armazenar todos os pontos descobertos no modelo.

### ModelScanner

Responsável por percorrer o modelo IEC e criar lista de pontos de PointRegistry.

Traversal:

```
IedModel
LogicalDevice
LogicalNode
DataObject
DataAttribute
```

## Dispositivos simulados

Tipos suportados até o momento:

```
DeviceBase
Breaker (XCBR)
Switch (XSWI)
MeasurementDevice (MMXU)
CSWI Controller
```

### DeviceManager

Registry central de dispositivos.

Estrutura:

```
Breakers
Switches
Measurements
Controllers
```

### DeviceBuilder

Transforma `DevicePoints` em dispositivos simulados.

Fluxo:

```
PointRegistry
→ DeviceBuilder
→ DeviceManager
```


### SimulationEngine

Responsável pela evolução temporal da simulação.

Componentes:

```
SimulationClock
SimulationEngine
```


### SimulationService

Executa o loop da simulação.

Tipo:

```
BackgroundService
```


### IEC Server Host

Wrapper da biblioteca:

```
IedServer
```

Responsabilidades:

```
start server
publish values
expor instancia do servidor
```


# 5. Regras de Governança e Testes (TDD)

## Protocolo de Testes unitários

Devem ser feitos testes unitários sobre o projeto "Iec61850Sim.Core" que é o projeto que detém todas as funcionalidades relevantes do projeto. Testes realizados nos clientes (Iec61850Sim.Web e Iec61850.Desktop) são desnecessários.

Os testes devem ser feitos em todos os métodos acessíveis (public). 

Os testes serão inseridos em projeto externo Iec61850Sim.Tests.

Os testes devem ser criados antes da criação de qualquer novo método acessível (TDD).

Ao alterar métodos acessíveis existentes, se o método ainda não tiver um teste associado, crie o teste. 

Novos métodos ou funcionalidades só serão considerados done (DoD) quando passarem em todos os testes existentes. 

Bibliotecas externas para testes:
- xUnitV3
- NSubstitute
- Bogus

## Protocolo de Testes de comunicação

Serão feitos manualmente a partir do Action.Net  (SCADA).


## Regras obrigatórias de desenvolvimento

1. Sempre verificar repositório oficial do projeto antes de escrever código: https://github.com/leokporto/Iec61850Sim
2. Sempre usar API real do libiec61850
3. Nunca inventar métodos
4. Sempre validar documentação oficial
5. Preferir soluções simples e robustas
6. Verificar repositório antes de escrever código
7. Sempre obedecer o protocolo de testes Unitários 

---

## Instruções de comportamento para IA

Ao desenvolver código:

1. Verificar documentação
2. Verificar repositório
3. Validar API .NET do libiec61850

Nunca assumir API da versão C.

---

## Registro de erros

Se um erro ocorrer:

1. registrar causa
2. registrar correção
3. atualizar esta documentação

---

## Documentação

Sempre que uma nova funcionalidade ou configuração for adicionada, deverá ser feita a inclusão da mesma no arquivo `CHANGELOG.md`.

Um texto deverá ser disponibilizada para inclusão neste arquivo.


# Backlog oficial do projeto

Itens registrados:


- [ ] Commands Simulation
- [ ] Parse scl files automatically
- [ ] Discover Ied Server model
- [ ] Add settings page
- [ ] Better Simulation UI
- [ ] Code refactoring
- [ ] Upload Scl files


---

# Licença

Este projeto é licenciado sob:

```
GNU GPL v3
```

Biblioteca utilizada:

```
libiec61850
```

também sob GPL v3.

