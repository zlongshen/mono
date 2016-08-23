using System;
using System.IO;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Mono.Btls.Interface;

namespace Mono.Btls
{
	static class BtlsCertSync
	{
		static void Main (string[] args)
		{
			if (!BtlsProvider.IsSupported ()) {
				Console.Error.WriteLine ("BTLS is not supported in this runtime!");
				Environment.Exit (255);
			}

			var configPath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			configPath = Path.Combine (configPath, ".mono");

			var oldStorePath = Path.Combine (configPath, "certs", "Trust");
			var newStorePath = Path.Combine (configPath, "certs", "NewTrust");

			if (!Directory.Exists (oldStorePath)) {
				Console.WriteLine ("Old trust store {0} does not exist.");
				Environment.Exit (255);
			}

			if (Directory.Exists (newStorePath))
				Directory.Delete (newStorePath, true);
			Directory.CreateDirectory (newStorePath);

			var oldfiles = Directory.GetFiles (oldStorePath, "*.cer");
			Console.WriteLine ("Found {0} files in the old store.", oldfiles.Length);

			foreach (var file in oldfiles) {
				Console.WriteLine ("Converting {0}.", file);
				var data = File.ReadAllBytes (file);
				using (var x509 = BtlsProvider.CreateNative (data, BtlsX509Format.DER)) {
					ConvertToNewFormat (newStorePath, x509);
				}
			}
		}

		static void ConvertToNewFormat (string root, BtlsX509 x509)
		{
			long hash = x509.GetSubjectNameHash ();

			string newName;
			int index = 0;
			do {
				newName = Path.Combine (root, string.Format ("{0:x8}.{1}", hash, index++));
			} while (File.Exists (newName));
			Console.WriteLine ("  new name: {0}", newName);

			using (var stream = new FileStream (newName, FileMode.Create))
				x509.ExportAsPEM (stream, true);
		}
	}
}
