using System.Drawing;

namespace Models;

public static class GenerationModel
{
    public record GenerationRequest(Guid RequestId, int? Size, int? Quality, int? Seed)
    {
        public void Deconstruct (out Guid requestId, out int? size, out int? quality, out int? seed)
        {
            requestId = RequestId;
            size = Size;
            quality = Quality;
            seed = Seed;
        }
    }

    public record GenerationResponse(Guid Id, string Result)
    {
        public void Deconstruct(out Guid id, out string result)
        {
            id = Id;
            result = Result;
        }
    }
}
