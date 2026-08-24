// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using Tabularius.Data;
using Tabularius.Interop.Dto;
using Amount = Tabularius.Data.Amount;
using AmountStyle = Tabularius.Data.AmountStyle;
using BalanceReportItem = Tabularius.Data.BalanceReportItem;
using Decimal = Tabularius.Interop.Dto.Decimal;
using MixedAmount = Tabularius.Data.MixedAmount;
using MixedAmountEntry = Tabularius.Data.MixedAmountEntry;

namespace Tabularius.Interop;

/// <remarks>
/// Copies the whole native object graph into managed values, so that the native result may be freed right after the
/// call.
/// </remarks>
internal static unsafe class BalanceReportMarshaller
{
    internal static BalanceReport Read(BalanceReportResult* result) => new(
        ReadItems(result->itemCount, result->items),
        ReadMixedAmount(result->totals));

    private static BalanceReportItem[] ReadItems(int count, Dto.BalanceReportItem* items)
    {
        if (items == null || count <= 0) return [];

        var result = new BalanceReportItem[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new BalanceReportItem(
                ReadString(items[i].accountName),
                items[i].indentationSteps,
                ReadMixedAmount(items[i].amount));
        }

        return result;
    }

    private static MixedAmount ReadMixedAmount(in Dto.MixedAmount native)
    {
        var entries = native.entries;
        var count = native.entryCount;
        if (entries == null || count <= 0) return new MixedAmount([]);

        var result = new MixedAmountEntry[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new MixedAmountEntry(ReadString(entries[i].key.commodity), ReadAmount(entries[i].value));
        }

        return new MixedAmount(result);
    }

    private static Amount ReadAmount(in Dto.Amount native) => new(
        ReadString(native.commodity),
        ReadQuantity(native.quantity),
        ReadStyle(native.style));

    private static AmountStyle ReadStyle(in Dto.AmountStyle native) => new(
        (Side)native.commoditySide,
        native.commoditySpaced,
        native.precision.isNatural
            ? AmountPrecision.NaturalPrecision
            : AmountPrecision.NewPrecision(native.precision.precision));

    private static decimal ReadQuantity(in Decimal native)
    {
        if (native.mantissa == null || native.mantissaLength == 0) return decimal.Zero;

        var bytes = new ReadOnlySpan<byte>(native.mantissa, checked((int)native.mantissaLength));
        return ToDecimal(new BigInteger(bytes, isUnsigned: false, isBigEndian: false), native.places);
    }

    /// <summary>Max mantissa (in bits) for a <see cref="Decimal"/>.</summary>
    private static readonly BigInteger MaxMantissa = (BigInteger.One << 96) - BigInteger.One;

    /// <remarks>Never rounds: throws if the value is not exactly representable as a <see cref="decimal"/>.</remarks>
    internal static decimal ToDecimal(BigInteger mantissa, byte places)
    {
        if (places > 28) throw new OverflowException(
            $"A quantity with {places} decimal places cannot be represented as a decimal value.");

        var magnitude = BigInteger.Abs(mantissa);
        if (magnitude > MaxMantissa) throw new OverflowException(
            $"A quantity with mantissa {mantissa} cannot be represented as a decimal value.");

        Span<byte> buffer = stackalloc byte[12];
        buffer.Clear();
        magnitude.TryWriteBytes(buffer, out _, isUnsigned: true, isBigEndian: false);

        return new decimal(
            BinaryPrimitives.ReadInt32LittleEndian(buffer),
            BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]),
            BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]),
            mantissa.Sign < 0,
            places);
    }

    private static string ReadString(byte* text) =>
        text == null ? "" : Marshal.PtrToStringUTF8((IntPtr)text) ?? "";
}
