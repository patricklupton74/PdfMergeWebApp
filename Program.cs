using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using PdfSharpCore.Pdf.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


//for giving user a pdf download
app.MapGet("/pdf/download", () =>
{
    //create new pdf
    var document = new PdfDocument();
    var page = document.AddPage();
    var gfx = XGraphics.FromPdfPage(page);
    var font = new XFont("Helvetica", 20);

    gfx.DrawString("pdf test", font, XBrushes.Black,
        new XRect(0, 0, page.Width, page.Height),
        XStringFormats.Center);

    using var stream = new MemoryStream();
    document.Save(stream);

    return Results.File(stream.ToArray(), "application/pdf", "test.pdf");
})
.WithName("GetPdfDownload")
.WithOpenApi();

//for returning html of pdf download page
app.MapGet("/pdf", () =>
{
    var html = @"<html>
        <head><title>click button to download pdf</title></head>
        <body>
            <h1>test string h1 PATRICK</h1>
            <p>test string p</p>
            <a href=""/pdf/download""><button>Download PDF</button></a>

            <h1>Upload PDFs</h1>
            <form action=""/pdf/upload"" method=""post"" enctype=""multipart/form-data"">
                <input type=""file"" name=""files"" accept="".pdf"" multiple required />
                <br><br>
                <button type=""submit"">Upload</button>
            </formm>
            
        </body>
        </html>";
    return Results.Content(html, "text/html");
})
.WithName("GetPdfHtml")
.WithOpenApi();

app.MapPost("/pdf/upload", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("no files uploaded");
    
    var form = await request.ReadFormAsync();
    var files = form.Files;

    if (files.Count == 0)
        return Results.BadRequest("no files uploaded");

    var outputDoc = new PdfDocument();
    //var pdfStreams = new List<byte[]>();
    
    foreach (var file in files)
    {
        if (file.ContentType != "application/pdf")
            return Results.BadRequest("only pdf files allowed");
    
        using var inputStream = new MemoryStream();
        await file.CopyToAsync(inputStream);
        //pdfStreams.Add(ms.ToArray());

        using var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
        {
            foreach (var page in inputDoc.Pages)
            {
                outputDoc.AddPage(page);
            }
        }   
    }

    using var outputStream = new MemoryStream();
    outputDoc.Save(outputStream);
    byte[] mergedPdfBytes = outputStream.ToArray();
    return Results.File(mergedPdfBytes, "application/pdf", "returnedfiles.pdf");
    //return Results.Ok($"recieved {files.Count} PDFs "+ Environment.NewLine + filenames);

})
.WithName("PostPdfUploads")
.WithOpenApi();


app.Run();
