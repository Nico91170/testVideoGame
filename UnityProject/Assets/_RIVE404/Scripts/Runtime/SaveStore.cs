using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Rive404.Domain;

namespace Rive404.Runtime
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int format = 1;
        public string payload;
        public string sha256;
    }

    /// <summary>Storage shared by Unity and the real filesystem tests; serializers are injected.</summary>
    public sealed class SaveStore
    {
        private readonly Func<GameState, string> serializeState;
        private readonly Func<string, GameState> deserializeState;
        private readonly Func<SaveEnvelope, string> serializeEnvelope;
        private readonly Func<string, SaveEnvelope> deserializeEnvelope;
        public string Path { get; private set; }
        public bool HasSave { get { return File.Exists(Path) || File.Exists(Path + ".bak"); } }

        public SaveStore(string folder, Func<GameState, string> serializeState,
            Func<string, GameState> deserializeState, Func<SaveEnvelope, string> serializeEnvelope,
            Func<string, SaveEnvelope> deserializeEnvelope)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Dossier de sauvegarde absent.");
            this.serializeState = serializeState ?? throw new ArgumentNullException(nameof(serializeState));
            this.deserializeState = deserializeState ?? throw new ArgumentNullException(nameof(deserializeState));
            this.serializeEnvelope = serializeEnvelope ?? throw new ArgumentNullException(nameof(serializeEnvelope));
            this.deserializeEnvelope = deserializeEnvelope ?? throw new ArgumentNullException(nameof(deserializeEnvelope));
            Path = System.IO.Path.Combine(folder, "rive404-save.json");
        }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var errors = StateRules.Validate(state);
            if (errors.Count != 0) throw new InvalidDataException(string.Join(" ", errors));
            string payload = serializeState(state);
            string text = serializeEnvelope(new SaveEnvelope { payload = payload, sha256 = Hash(payload) });
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temporary = Path + ".tmp";
            byte[] bytes = new UTF8Encoding(false).GetBytes(text);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Never replace the last good backup with a corrupt primary file.
            bool primaryValid = false;
            if (File.Exists(Path))
            {
                try { Read(Path); primaryValid = true; }
                catch (Exception error) when (IsRecoverable(error)) { }
            }
            if (!File.Exists(Path)) File.Move(temporary, Path);
            else
            {
                try { File.Replace(temporary, Path, primaryValid ? Path + ".bak" : null); }
                catch (PlatformNotSupportedException)
                {
                    if (primaryValid) File.Copy(Path, Path + ".bak", true);
                    File.Delete(Path);
                    File.Move(temporary, Path);
                }
            }
        }

        public bool TryLoad(out GameState state, out string message)
        {
            state = null;
            message = string.Empty;
            Exception primaryError = null;
            if (File.Exists(Path))
            {
                try { state = Read(Path); return true; }
                catch (Exception error) when (IsRecoverable(error)) { primaryError = error; }
            }
            if (File.Exists(Path + ".bak"))
            {
                try
                {
                    state = Read(Path + ".bak");
                    message = "Sauvegarde principale indisponible ; copie de secours chargée.";
                    return true;
                }
                catch (Exception error) when (IsRecoverable(error))
                { message = "Sauvegarde et copie de secours illisibles : " + error.Message; return false; }
            }
            message = primaryError == null ? "Aucune sauvegarde disponible." : "Sauvegarde illisible : " + primaryError.Message;
            return false;
        }

        private GameState Read(string path)
        {
            var info = new FileInfo(path);
            if (info.Length == 0 || info.Length > 8 * 1024 * 1024) throw new InvalidDataException("Taille invalide.");
            var envelope = deserializeEnvelope(File.ReadAllText(path, Encoding.UTF8));
            if (envelope == null || envelope.format != 1 || string.IsNullOrEmpty(envelope.payload) ||
                !string.Equals(Hash(envelope.payload), envelope.sha256, StringComparison.Ordinal))
                throw new InvalidDataException("Intégrité de sauvegarde invalide.");
            var state = deserializeState(envelope.payload);
            if (state == null) throw new InvalidDataException("Progression absente.");
            state = StateRules.Migrate(state);
            var errors = StateRules.Validate(state);
            if (errors.Count != 0) throw new InvalidDataException(string.Join(" ", errors));
            return StateRules.Normalize(state);
        }

        private static string Hash(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var result = new StringBuilder(64);
                foreach (byte value in bytes) result.Append(value.ToString("x2"));
                return result.ToString();
            }
        }

        private static bool IsRecoverable(Exception error)
        {
            // Includes serializer-specific JSON errors, while preserving process/cancellation failures.
            return !(error is OutOfMemoryException) && !(error is OperationCanceledException);
        }
    }
}
