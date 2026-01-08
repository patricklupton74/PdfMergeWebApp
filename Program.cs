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
app.UseStaticFiles();


//for returning html of merge page
app.MapGet("/merge", (IHostEnvironment env) => {
    var filePath = Path.Combine(env.ContentRootPath, "wwwroot/html", "merge.html");
    
    if (!File.Exists(filePath))
    {
        return Results.NotFound($"HTML file not found at: {filePath}");
    }

    return Results.File(filePath, "text/html");
}
);

//merge (post) endpoint, takes input from form, merges files and returns as download
app.MapPost("/merge", async (HttpRequest request) =>
{
    //validation for pdf files only
    if (!request.HasFormContentType)
        return Results.BadRequest("expected form data");
    var form = await request.ReadFormAsync();
    var files = form.Files;

    //presence check
    if (files.Count == 0)
        return Results.BadRequest("no files uploaded");

    //new output pdf to merge into
    var outputDoc = new PdfDocument();
    //var pdfStreams = new List<byte[]>();
    
    foreach (var file in files)
    {
        //ensuring every file is a pdf
        if (file.ContentType != "application/pdf")
            return Results.BadRequest("only pdf files allowed");
        //memory stream for manipulating specific input pdf
        using var inputStream = new MemoryStream();
        await file.CopyToAsync(inputStream);
        //pdfStreams.Add(ms.ToArray());
        //opening using reader to add every page to output pdf
        using var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
        {
            foreach (var page in inputDoc.Pages)
            {
                outputDoc.AddPage(page);
            }
        }   
    }
    //write output to memory stream to convert to bytes then output
    using var outputStream = new MemoryStream();
    outputDoc.Save(outputStream);
    byte[] mergedPdfBytes = outputStream.ToArray();
    //return file as download
    return Results.File(mergedPdfBytes, "application/pdf", "returnedfiles.pdf");
    //return Results.Ok($"recieved {files.Count} PDFs "+ Environment.NewLine + filenames);

})
.WithName("PostPdfMerge")
.WithOpenApi();


app.Run();
