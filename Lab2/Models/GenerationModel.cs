using System.Drawing;

namespace Models;

public static class GenerationModel
{
    public record GenerationRequest(int RequestId, int? Size, int? Quality, int? Seed)
    {
        public void Deconstruct (out int requestId, out int? size, out int? quality, out int? seed)
        {
            requestId = RequestId;
            size = Size;
            quality = Quality;
            seed = Seed;
        }
    }

    public record GenerationResponse(int Id, string Result)
    {
        public void Deconstruct(out int id, out string result)
        {
            id = Id;
            result = Result;
        }
    }
}
