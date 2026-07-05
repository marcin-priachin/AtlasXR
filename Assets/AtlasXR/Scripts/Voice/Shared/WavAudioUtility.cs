using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AtlasXR.Voice.Shared
{
    public static class WavAudioUtility
    {
        public static byte[] Encode(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                throw new ArgumentNullException(nameof(audioClip));
            }

            var samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);
            return Encode(samples, audioClip.frequency, audioClip.channels);
        }

        public static byte[] CreateSilence(float durationSeconds, int sampleRate, int channels)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate) * channels);
            return Encode(new float[sampleCount], sampleRate, channels);
        }

        public static byte[] CreateTone(float durationSeconds, int sampleRate, int channels, float frequency, float amplitude)
        {
            var frames = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            var samples = new float[frames * channels];
            var clampedAmplitude = Mathf.Clamp01(amplitude);

            for (var frame = 0; frame < frames; frame++)
            {
                var sample = Mathf.Sin(2f * Mathf.PI * frequency * frame / sampleRate) * clampedAmplitude;
                for (var channel = 0; channel < channels; channel++)
                {
                    samples[frame * channels + channel] = sample;
                }
            }

            return Encode(samples, sampleRate, channels);
        }

        public static AudioClip Decode(byte[] wavData, string clipName)
        {
            if (wavData == null || wavData.Length < 44)
            {
                throw new ArgumentException("WAV data is empty or too short.", nameof(wavData));
            }

            using (var reader = new BinaryReader(new MemoryStream(wavData)))
            {
                var riff = ReadChunkId(reader);
                reader.ReadUInt32();
                var wave = ReadChunkId(reader);
                if (riff != "RIFF" || wave != "WAVE")
                {
                    throw new InvalidOperationException(
                        $"Audio data is not a WAV file. Header was '{riff}/{wave}'. Check the text-to-speech response format.");
                }

                short channels = 0;
                int sampleRate = 0;
                short bitsPerSample = 0;
                byte[] sampleBytes = null;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var chunkId = ReadChunkId(reader);
                    var chunkSize = reader.ReadUInt32();
                    var remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
                    if (chunkId == "data" && chunkSize == uint.MaxValue)
                    {
                        chunkSize = CheckedRemainingSize(remainingBytes);
                    }

                    if (chunkSize > remainingBytes)
                    {
                        throw new InvalidOperationException(
                            $"WAV chunk '{chunkId}' declares {chunkSize} bytes, but only {remainingBytes} bytes remain.");
                    }

                    if (chunkId == "fmt ")
                    {
                        var audioFormat = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        sampleRate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt16();
                        bitsPerSample = reader.ReadInt16();

                        if (chunkSize > 16)
                        {
                            reader.BaseStream.Position += chunkSize - 16;
                        }

                        if (audioFormat != 1)
                        {
                            throw new InvalidOperationException("Only PCM WAV audio is supported.");
                        }
                    }
                    else if (chunkId == "data")
                    {
                        sampleBytes = reader.ReadBytes(CheckedChunkSize(chunkSize));
                    }
                    else
                    {
                        reader.BaseStream.Position += chunkSize;
                    }

                    if ((chunkSize & 1) == 1 && reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        reader.BaseStream.Position += 1;
                    }
                }

                if (sampleBytes == null || channels <= 0 || sampleRate <= 0)
                {
                    throw new InvalidOperationException("WAV data is missing required format or sample chunks.");
                }

                var samples = DecodeSamples(sampleBytes, bitsPerSample);
                var clip = AudioClip.Create(
                    string.IsNullOrWhiteSpace(clipName) ? "Speech" : clipName,
                    samples.Length / channels,
                    channels,
                    sampleRate,
                    false);
                clip.SetData(samples, 0);
                return clip;
            }
        }

        private static byte[] Encode(float[] samples, int sampleRate, int channels)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                var dataSize = samples.Length * 2;

                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                for (var i = 0; i < samples.Length; i++)
                {
                    var clampedSample = Mathf.Clamp(samples[i], -1f, 1f);
                    writer.Write((short)(clampedSample * short.MaxValue));
                }

                return stream.ToArray();
            }
        }

        private static string ReadChunkId(BinaryReader reader)
        {
            return Encoding.ASCII.GetString(reader.ReadBytes(4));
        }

        private static int CheckedChunkSize(uint chunkSize)
        {
            if (chunkSize > int.MaxValue)
            {
                throw new InvalidOperationException($"WAV chunk is too large to decode: {chunkSize} bytes.");
            }

            return (int)chunkSize;
        }

        private static uint CheckedRemainingSize(long remainingBytes)
        {
            if (remainingBytes < 0 || remainingBytes > uint.MaxValue)
            {
                throw new InvalidOperationException($"WAV remaining data size is invalid: {remainingBytes} bytes.");
            }

            return (uint)remainingBytes;
        }

        private static float[] DecodeSamples(byte[] sampleBytes, short bitsPerSample)
        {
            if (bitsPerSample != 16)
            {
                throw new InvalidOperationException("Only 16-bit PCM WAV audio is supported.");
            }

            var samples = new float[sampleBytes.Length / 2];
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = BitConverter.ToInt16(sampleBytes, i * 2);
                samples[i] = sample / 32768f;
            }

            return samples;
        }
    }
}
