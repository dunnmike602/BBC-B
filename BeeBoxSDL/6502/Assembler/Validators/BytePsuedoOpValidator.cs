namespace BeeBoxSDL._6502.Assembler.Validators;

using System.Text;
using Extensions;

public class BytePsuedoOpValidator : AddressModeValidator
{
    public override void Validate(Operation operation)
    {
        if (operation.Mnemonic.Trim() == Data.BYTE)
        {
            operation.HasBeenValidated = true;

            var arguments = operation.Argument!.Split([','], StringSplitOptions.RemoveEmptyEntries);
            var bytes = new List<byte>();

            foreach (var argument in arguments)
            {
                if (argument.Contains("\""))
                {
                    var utf8Bytes = Encoding.UTF8.GetBytes(argument.Replace("\"", string.Empty));

                    bytes.AddRange(utf8Bytes);
                }
                else
                {
                    var byteValue = argument.ConvertToByte();

                    if (byteValue.HasValue)
                    {
                        bytes.Add(byteValue.Value);
                    }
                }
            }

            if (bytes.Count == 0)
            {
                operation.ErrorMessage = "Invalid format for .BYTE psuedo-operation.";
                return;
            }

            operation.ActualOpCode = bytes[0];

            if (bytes.Count > 1)
            {
                operation.Parameters = new byte[bytes.Count - 1];

                bytes.RemoveAt(0);
                bytes.CopyTo(operation.Parameters);
            }
        }
    }
}