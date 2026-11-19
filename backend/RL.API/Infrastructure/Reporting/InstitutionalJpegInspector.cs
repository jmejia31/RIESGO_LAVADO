using System.Buffers.Binary;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Inspector seguro para imágenes JPEG en formato institucional B06.
/// Valida la estructura de segmentos SOF0/SOF2 para extraer dimensiones y relación de aspecto.
/// </summary>
internal static class InstitutionalJpegInspector
{
    internal static (int Width, int Height, int Components) InspectJpeg(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            throw new InvalidOperationException("La imagen JPEG está vacía o truncada.");

        if (bytes[0] != 0xFF || bytes[1] != 0xD8)
            throw new InvalidOperationException("La imagen configurada no es un JPEG válido (firma SOI ausente).");

        var offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            if (bytes[offset++] != 0xFF)
                throw new InvalidOperationException("Estructura de segmentos JPEG corrupta (marcador 0xFF esperado).");

            while (offset < bytes.Length && bytes[offset] == 0xFF)
                offset++;

            if (offset >= bytes.Length)
                break;

            var marker = bytes[offset++];

            // Standalone markers without payload
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7))
                continue;

            if (marker == 0xD9) // EOI
                break;

            if (offset + 2 > bytes.Length)
                throw new InvalidOperationException("Segmento JPEG truncado.");

            var segmentLength = checked((int)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2)));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
                throw new InvalidOperationException("Longitud de segmento JPEG inválida.");

            var payloadStart = offset + 2;
            var payloadLength = segmentLength - 2;

            // SOF0 (Baseline), SOF1 (Extended Sequential), SOF2 (Progressive)
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (payloadLength < 6)
                    throw new InvalidOperationException("Segmento SOF JPEG demasiado corto.");

                var precision = bytes[payloadStart];
                var height = checked((int)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(payloadStart + 1, 2)));
                var width = checked((int)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(payloadStart + 3, 2)));
                var components = bytes[payloadStart + 5];

                if (precision != 8)
                    throw new InvalidOperationException($"Precisión JPEG {precision} bits no soportada (se requiere 8 bits).");

                if (width <= 0 || height <= 0)
                    throw new InvalidOperationException("Dimensiones JPEG inválidas en el segmento SOF.");

                return (width, height, components);
            }

            if (marker == 0xDA) // Start of Scan (SOS) - image data follows
                break;

            offset += segmentLength;
        }

        throw new InvalidOperationException("No se encontró un segmento de cuadro (SOF) válido en la imagen JPEG.");
    }
}
