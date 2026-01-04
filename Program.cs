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


//for returning html of pdf download page
app.MapGet("/merge", (IHostEnvironment env) => {
// Path.Combine is safer for cross-platform (Windows vs Linux)
    var filePath = Path.Combine(env.ContentRootPath, "wwwroot/html", "merge.html");
    
    if (!File.Exists(filePath))
    {
        return Results.NotFound($"HTML file not found at: {filePath}");
    }

    return Results.File(filePath, "text/html");
}
);
/*
app.MapGet("/pdf", () =>
{
    var html = @"<html>
<head>
    <title>click button to download pdf</title>
    <script src=""https://cdn.jsdelivr.net/npm/sortablejs@1.15.0/Sortable.min.js""></script>
</head>
<body>

    <h1>MERGE PDFS</h1>
    <form id=""mergeForm"">
        <input id=""fileInput"" type=""file"" name=""files"" accept="".pdf"" multiple required />
        <ul id=""fileList"" style=""cursor: grab;""></ul>
        <button type=""submit"">MERGE</button>
    </form>

    <script>
    const fileInput = document.getElementById(""fileInput"");
    const fileList = document.getElementById(""fileList"");
    const mergeForm = document.getElementById(""mergeForm"");
    let filesArray = [];

    fileInput.addEventListener(""change"", (event) => {
        filesArray = Array.from(event.target.files);
        renderList();
    });

    function renderList() {
        fileList.innerHTML = """";
        filesArray.forEach((file, index) => {
            const li = document.createElement(""li"");
            li.textContent = file.name;
            li.fileReference = file; 
            fileList.appendChild(li);
        });
    }

    new Sortable(fileList, {
        animation: 150,
        onEnd: () => {
            const newOrder = [];
            fileList.querySelectorAll(""li"").forEach(li => {
                newOrder.push(li.fileReference);
            });
            filesArray = newOrder;
        }
    });

    mergeForm.addEventListener(""submit"", async (e) => {
        e.preventDefault();
        if (filesArray.length === 0) return;

        const formData = new FormData();
        filesArray.forEach(file => {
            formData.append(""files"", file);
        });

        try {
            const response = await fetch(""/pdf/merge"", {
                method: ""POST"",
                body: formData
            });
            
            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement(""a"");
                a.href = url;
                a.download = ""merged.pdf"";
                document.body.appendChild(a);
                a.click();
                a.remove();
            }
        } catch (error) {
            console.error(""Error:"", error);
        }
    });
    </script>
    
</body>
</html>";
    return Results.Content(html, "text/html");
})
.WithName("GetPdfHtml")
.WithOpenApi();
*/

/*
var fileList = """";
            document.addEventListener(""DOMContentLoaded"", init, false);

            function init() {
                document.querySelector('#fileInput').addEventListener('change', handleFileSelect, false);
                fileList = document.querySelector(""#fileList"");
            }

            function handleFileSelect(e) {
                if(!e.target.files) return;

                fileList.innerHTML = """";

                var files = e.target.files;
                foreach
                fileList.innerHTML = """""";
                filesArray.forEach((file, index) => {
                    const li = document.createElement(""li"")
                    li.textContent = file.name;
                    li.dataset.index = index;
                    fileList.appendChild(li);
                });
            }

<script>
            const fileInput = document.getElementById(""fileInput"");
            const fileList = document.getElementById(""fileList"");
            let filesArray = [];

            fileInput.addEventListener(""change"", (e) => {
                filesArray = Array.form(e.target.files);
                renderList();
            });

            function renderList() {
                fileList.innerHTML = """";
                filesArray.forEach((file, index) => {
                    const li = document.createElement(""li"")
                    li.textContent = file.name;
                    li.dataset.index = index;
                    fileList.appendChild(li);
                });
            }

            </script>
*/

//merge endpoint, takes input from form, merges files and returns as download
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
