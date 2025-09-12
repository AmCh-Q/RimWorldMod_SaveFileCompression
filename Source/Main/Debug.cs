using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Verse;

namespace SaveFileCompression;

public enum LogLevel
{
	Off,
	Critical,
	Error,
	Warning,
	Information,
	Trace
}

public static class Debug
{
	private static readonly ConcurrentQueue<(LogLevel, object)> messagesQueue = new();

	public static readonly MethodInfo m_Clear = typeof(Log).GetMethod(nameof(Verse.Log.Clear));
	public static readonly MethodInfo g_Messages = typeof(Log).GetProperty(nameof(Verse.Log.Messages)).GetGetMethod();

	internal static void Patch(Harmony harmony)
	{
		HarmonyMethod h_Clear = new(typeof(Debug).GetMethod(nameof(Clear)));
		harmony.Patch(m_Clear, prefix: h_Clear);
		HarmonyMethod h_Flush = new(typeof(Debug).GetMethod(nameof(Flush)));
		harmony.Patch(g_Messages, prefix: h_Flush);
	}

	public static LogLevel logLevel = LogLevel.Critical;

	public static bool IsEnabled(LogLevel level)
		=> level <= logLevel && level != LogLevel.Off;

	public static string MessageHeader => "[Save File Compression]: ";

	public static void Log(LogLevel level, object message)
	{
		if (!UnityData.IsInMainThread)
			messagesQueue.Enqueue((level, message));
		else
			LogDirect(level, message);
	}

	public static void Critical(object message)
		=> Log(LogLevel.Critical, message);

	public static void Error(object message)
		=> Log(LogLevel.Error, message);

	public static void Warning(object message)
		=> Log(LogLevel.Warning, message);

	public static void Information(object message)
		=> Log(LogLevel.Information, message);

	public static void Trace(object message)
		=> Log(LogLevel.Trace, message);

	public static void Clear() => messagesQueue.Clear();

	public static void Flush()
	{
		if (messagesQueue.IsEmpty || !UnityData.IsInMainThread)
			return;
		while (messagesQueue.TryDequeue(out (LogLevel, object) sourceLevel))
			LogDirect(sourceLevel.Item1, sourceLevel.Item2);
	}

	private static void LogDirect(LogLevel level, object source)
	{
		if (!IsEnabled(level))
			return;
		string message = Evaluate(source);
		switch (level)
		{
			case LogLevel.Off:
				return;

			case LogLevel.Trace:
			case LogLevel.Information:
				Verse.Log.Message(message);
				return;

			case LogLevel.Warning:
				Verse.Log.Warning(message);
				return;

			case LogLevel.Error:
			case LogLevel.Critical:
			default:
				Verse.Log.Error(message);
				return;
		}
	}

	private static string Evaluate(object source) => source switch
	{
		string message => MessageHeader + message,
		Func<string> d_message => MessageHeader + d_message(),
		_ => throw new ArgumentException(
			$"Unsupported type: {source?.GetType().Name ?? "null"}",
			nameof(source)),
	};
}