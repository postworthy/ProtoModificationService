using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProtoModificationService.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProtoModificationService.Controllers
{
    [Route("api/[controller]")]
    public class ProtobufController : Controller
    {
        // GET ProtobufController/Get
        [HttpGet]
        public string Get()
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = Request.Scheme,
                Host = Request.Host.Host,
                Port = Request.Host.Port.GetValueOrDefault(80),
                Path = Request.Path.ToString(),
                Query = Request.QueryString.ToString()
            };

            return $"POST a Protobuf! (ex: curl --location --request POST '{uriBuilder.Uri}' --form 'b64Proto=\"YOUR_BASE64_PROTOBUF_GOES_HERE\"' --form 'find=\"FIND_IN_PROTO\" --form 'replace=\"REPLACE_IN_PROTO\"')";
        }
        // POST: ProtobufController/Create
        [HttpPost]
        public async Task<byte[]> Create([FromForm]string b64Proto, [FromForm] string find, [FromForm] string replace)
        {
            //byte[] proto = new byte[(int)Request.ContentLength];
            //await Request.Body.ReadAsync(proto, 0, proto.Length);
            byte[] proto = Convert.FromBase64String(b64Proto);
            string outputProtoFilePath = Path.GetTempFileName();
            await System.IO.File.WriteAllBytesAsync(outputProtoFilePath, proto);
            var pH = new ProtoHelper();
            var result = await pH.Run(outputProtoFilePath, new[] { new ModifyObj { Find = find, Replace = replace } });
            return result;
        }
    }
}
