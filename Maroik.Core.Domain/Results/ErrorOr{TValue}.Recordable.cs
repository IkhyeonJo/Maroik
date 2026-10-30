using Maroik.Core.Domain.Results.Interfaces;

namespace Maroik.Core.Domain.Results;

public readonly partial record struct ErrorOr<TValue>
{
    /// <inheritdoc/>
    public TOutput GetRecording<TOutput>(IRecordingSerializer<TOutput> serializer)
    {
        return IsError ? serializer.SerializeErrors(Errors) : serializer.SerializeValue(Value);
    }
}
