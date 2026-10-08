var builder = DistributedApplication.CreateBuilder(args);

// var sql = builder.AddSqlServer("sql")
//                  .AddDatabase("sqldb");

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator()
                     .AddBlobs("blobs");

var mailcatcher = builder.AddContainer("mailcatcher", "mailhog/mailhog")
                         .WithHttpEndpoint(targetPort: 8025, name: "web")
                         .WithEndpoint(targetPort: 1025, name: "smtp");

builder.AddProject<Projects.Roster_Web>("web")
       // .WithReference(sql)
       .WithReference(storage)
       .WithEnvironment("Smtp__Host", "localhost")
       .WithEnvironment("Smtp__Port", "1025");

builder.Build().Run();
