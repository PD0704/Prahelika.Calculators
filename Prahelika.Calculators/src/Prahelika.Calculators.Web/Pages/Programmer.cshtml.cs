using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Programmer;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class ProgrammerModel(IProgrammerCalculator calculator) : CalculatorPageModel
{
    [BindProperty(SupportsGet = true)]
    public ProgrammerTool Tool { get; set; } = ProgrammerTool.Convert;

    // The base that the typed numbers are written in.
    [BindProperty]
    public NumberBase InputBase { get; set; } = NumberBase.Decimal;

    [BindProperty]
    public string? Number { get; set; }

    [BindProperty]
    public string? Left { get; set; }

    [BindProperty]
    public BitwiseOperation Operation { get; set; } = BitwiseOperation.And;

    [BindProperty]
    public string? Right { get; set; }

    public string Description => Tool switch
    {
        ProgrammerTool.Convert => "Write a number in binary, octal, decimal and hexadecimal. Numbers are 64-bit. You can type the prefixes 0b, 0o or 0x, and spaces or underscores between digits.",
        ProgrammerTool.Bitwise => "AND, OR, XOR, NOT and shifts on 64-bit numbers. Both numbers, and the shift amount, are read in the base you choose.",
        _ => string.Empty
    };

    public static string OperationName(BitwiseOperation operation) => operation switch
    {
        BitwiseOperation.And => "AND (1 only if both bits are 1)",
        BitwiseOperation.Or => "OR (1 if either bit is 1)",
        BitwiseOperation.Xor => "XOR (1 if the bits differ)",
        BitwiseOperation.Not => "NOT (flip every bit, first number only)",
        BitwiseOperation.ShiftLeft => "Shift left (second number is the amount)",
        BitwiseOperation.ShiftRight => "Shift right (keeps the sign)",
        _ => operation.ToString()
    };

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please choose a valid base and operation.");
            return;
        }

        if (Tool == ProgrammerTool.Convert)
        {
            ShowConversion();
        }
        else
        {
            ShowBitwise();
        }
    }

    private void ShowConversion()
    {
        string text = Clean(Number);
        if (text.Length == 0)
        {
            Fail("Please enter a number.");
            return;
        }

        Show(calculator.ConvertBase(text, InputBase));
    }

    private void ShowBitwise()
    {
        string left = Clean(Left);
        string right = Clean(Right);

        if (left.Length == 0)
        {
            Fail("Please enter the first number.");
            return;
        }

        if (right.Length == 0 && Operation != BitwiseOperation.Not)
        {
            Fail("Please enter the second number.");
            return;
        }

        var leftNumber = calculator.Parse(left, InputBase);
        if (!leftNumber.IsSuccess)
        {
            Fail(leftNumber.Message ?? "The first number is not valid.");
            return;
        }

        var rightNumber = calculator.Parse(right.Length == 0 ? "0" : right, InputBase);
        if (!rightNumber.IsSuccess)
        {
            Fail(rightNumber.Message ?? "The second number is not valid.");
            return;
        }

        Show(calculator.Bitwise(leftNumber.Value, Operation, rightNumber.Value));
    }

    // Spaces and underscores are only there to make long numbers readable, so they are removed first.
    private static string Clean(string? text) =>
        (text ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Trim();

    // Writes the four bases as rows. (Named Show so it matches the other pages, but it takes a NumberConversion.)
    private void Show(CalcResult<NumberConversion> result)
    {
        if (!result.IsSuccess)
        {
            Fail(string.IsNullOrWhiteSpace(result.Message) ? "That calculation could not be done." : result.Message);
            return;
        }

        NumberConversion number = result.Value!;
        var view = new ResultView();
        view.Details.Add(new("Decimal", number.Decimal));
        view.Details.Add(new("Binary", GroupBits(number.Binary)));
        view.Details.Add(new("Octal", number.Octal));
        view.Details.Add(new("Hexadecimal", number.Hexadecimal));
        Display(view);

        Note = "Numbers are 64-bit. A negative number is shown as its 64 bits in binary, octal and hexadecimal.";
    }

    // "11111111" becomes "1111 1111": groups of four, counted from the right.
    private static string GroupBits(string bits)
    {
        var groups = new List<string>();

        for (int end = bits.Length; end > 0; end -= 4)
        {
            groups.Insert(0, bits[Math.Max(0, end - 4)..end]);
        }

        return string.Join(" ", groups);
    }
}