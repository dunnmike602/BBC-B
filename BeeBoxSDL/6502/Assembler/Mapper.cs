namespace BeeBoxSDL._6502.Assembler;

using Interfaces;

public class Mapper : IMapper
{
    public void MapAndValidate(Operation[] operations)
    {
        foreach (var operation in operations.Where(operation => !operation.OperationIsCommentOrLabel()))
        {
            Data.MapParameter(operation);

            if (string.IsNullOrWhiteSpace(operation.ErrorMessage))
            {
                Data.ValidateParameter(operation);
            }
        }
    }
}