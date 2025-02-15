// 		public async Task SendRangeAsync(Stream stream, long fileLength, string? file, string? contentType)
// 		{
// 			var rangeString = Headers["range"];
// 			if (rangeString == null)
// 			{
// 				if (!string.IsNullOrEmpty(file))
// 					await InternalSendFileAsync(file);
// 				else
// 					await SendStreamAsync(stream, contentType, DateTime.Now.ToUniversalTime().ToString("r"), true);
// 				return;
// 			}

// 			rangeString = rangeString.Substring(rangeString.IndexOf("bytes=") + 6);
// 			var minus = rangeString.IndexOf('-');
// 			long start = 0;
// 			var end = fileLength - 1;
// 			if (minus == 0)
// 				end = long.Parse(rangeString.Substring(1));
// 			else if (minus == rangeString.Length - 1)
// 				start = long.Parse(rangeString.Substring(0, minus));
// 			else
// 			{
// 				start = long.Parse(rangeString.Substring(0, minus));
// 				end = long.Parse(rangeString.Substring(minus + 1));
// 			}

// 			var contentLength = end - start + 1;
// 			if (string.IsNullOrEmpty(contentType))
// 				contentType = "video/mp4";
// 			var headerString =
// $@"{HttpResponseString} 206 Partial Content
// ETag: ""0815""
// Accept-Ranges: bytes
// Content-Length: {contentLength}
// Content-Range: bytes {start}-{end}/{fileLength}
// Keep-Alive: timeout=5, max=99
// Connection: Keep-Alive
// Content-Type: {contentType}

// ";
// 			Logger.Current.LowTrace(() => $"{Id} {headerString}");
// 			var vorspannBuffer = ASCIIEncoding.ASCII.GetBytes(headerString);
// 			await WriteAsync(vorspannBuffer, 0, vorspannBuffer.Length);
// 			var bytes = new byte[40000];
// 			var length = end - start;
// 			stream.Seek(start, SeekOrigin.Begin);
// 			long completeRead = 0;
// 			while (true)
// 			{
// 				var read = await stream.ReadAsync(bytes, 0, Math.Min(bytes.Length, (int)(contentLength - completeRead)));
// 				if (read == 0)
// 					return;
// 				completeRead += read;
// 				await WriteAsync(bytes, 0, read);
// 				if (completeRead == contentLength)
// 					return;
// 			}
// 		}