using Iec61850Sim.Core.Comtrade.Models;

namespace Iec61850Sim.Core.Comtrade;

/// <summary>
/// Opções de geração COMTRADE em uso, compartilhadas entre todos os circuitos Blazor (singleton).
/// Guarda um snapshot imutável: alterações substituem a referência inteira.
/// </summary>
public sealed class ComtradeSettings
{
    private volatile ComtradeOptions _current = new();

    public ComtradeOptions Current
    {
        get => _current;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = value.Waveform.Validate();
            if (errors.Count > 0)
                throw new ArgumentException(string.Join(" ", errors), nameof(value));

            _current = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Disparado após <see cref="Current"/> ser substituído.</summary>
    public event Action? Changed;
}
