### Fix — Command simulation: correção do ModelScanner e publicação imediata após comando

13/03/2026
- `DevicePoint`: removida propriedade `BaseReference` (não utilizada em nenhum lookup).

- `ModelScanner`: corrigida colisão entre pontos CO e ST do mesmo Data Object.
  A chave interna do dicionário passou a incluir o FC no formato `LD/LN.DO[FC]`,
  permitindo que o ponto ST do CSWI seja registrado separadamente do ponto CO,
  e portanto corretamente encontrado pelo DeviceBuilder.

- `IecServerHost`: `Publish()` refatorado para delegar a uma sobrecarga
  `Publish(IEnumerable<DevicePoint>)` que contém a lógica de publicação.
  O `ControlHandler` agora chama esta sobrecarga passando apenas o `Position`
  do CSWI e o `Position` do XCBR associado, garantindo publicação imediata
  do feedback de estado ao SCADA.

- `IecServerHost.PublishValue`: adicionado tratamento específico para atributos
  do tipo `DataAttributeType.CODEDENUM` (Dbpos), que requerem `MMS_BIT_STRING`
  de 2 bits via `NewBitString(2)` + `BitStringFromUInt32BigEndian`.
  O uso de `UpdateInt32AttributeValue` para este tipo não causava erro mas
  silenciosamente não atualizava o valor no servidor IEC.

- `ControlCommandProcessor`: lógica de aplicação de posição extraída para
  método privado `ApplyPosition`, eliminando duplicação entre as sobrecargas
  de `Operate`. Removida atualização indevida do ponto CO (`CommandController`).

- `IecServerHost.SelectStateChanged`: removida execução de comando no evento
  de deselect, que causava abertura indevida do breaker no fluxo SBO.