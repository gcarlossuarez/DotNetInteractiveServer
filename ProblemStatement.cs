using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
public static class ProblemStatement
{
    public static IResult Read(string root, string problemId)
    {
        if (!Regex.IsMatch(problemId, @"\A[A-Za-z0-9_-]+\z")) return Results.BadRequest("Identificador inválido.");
        try
        {
            var folder = Path.Combine(root, problemId);
            if (!Directory.Exists(folder)) return Results.NotFound();
            foreach (var p in new[] { root, folder })
                if ((File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) return Results.BadRequest("Ruta no permitida.");
            var files = Directory.GetFiles(folder, "*.docx").Where(p => !Path.GetFileName(p).StartsWith("~$")).ToArray();
            var chosen = files.FirstOrDefault(p => Path.GetFileName(p).Equals("enunciado.docx", StringComparison.OrdinalIgnoreCase));
            if (chosen == null && files.Length > 1) return Results.Conflict("Hay varios Word. El docente debe nombrar enunciado.docx al archivo que desea mostrar.");
            chosen ??= files.SingleOrDefault();
            if (chosen == null) return Results.NotFound();
            if ((File.GetAttributes(chosen) & FileAttributes.ReparsePoint) != 0) return Results.BadRequest("Archivo no permitido.");
            if (new FileInfo(chosen).Length > 10 * 1024 * 1024) return Results.BadRequest("El Word supera 10 MB.");
            using var zip = ZipFile.OpenRead(chosen);
            var entry = zip.GetEntry("word/document.xml");
            if (entry == null || entry.Length > 4 * 1024 * 1024) return Results.BadRequest("Documento Word no compatible o demasiado grande.");
            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4 * 1024 * 1024 });
            var xml = XDocument.Load(reader);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            string Paragraph(XElement p) => string.Concat(p.Descendants().Select(e => e.Name == w + "t" ? e.Value : e.Name == w + "tab" ? "\t" : (e.Name == w + "br" || e.Name == w + "cr") ? "\n" : ""));
            var body = xml.Root?.Element(w + "body");
            if (body == null) return Results.BadRequest("El documento no contiene texto compatible.");
            var blocks = body.Elements().Select(e => e.Name == w + "tbl"
                ? string.Join("\n", e.Elements(w + "tr").Select(row => string.Join(" | ", row.Elements(w + "tc").Select(cell => string.Join("\n", cell.Elements(w + "p").Select(Paragraph))))))
                : e.Name == w + "p" ? Paragraph(e) : "");
            return Results.Json(new { fileName = Path.GetFileName(chosen), text = string.Join("\n\n", blocks), simplified = true });
        }
        catch (InvalidDataException) { return Results.BadRequest("El archivo no es un .docx válido."); }
        catch (XmlException) { return Results.BadRequest("El contenido del Word no es válido."); }
        catch (IOException) { return Results.Problem("No se pudo leer el Word. Puede estar bloqueado por otro programa."); }
        catch (UnauthorizedAccessException) { return Results.Problem("No hay permiso de lectura del Word."); }
    }
}

