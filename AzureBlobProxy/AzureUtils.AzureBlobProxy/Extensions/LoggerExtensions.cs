using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace AzureUtils.AzureBlobProxy.Extensions;

[UsedImplicitly]
internal static class LoggerExtensions
{
    public static void ExecutingBlobStorageResult(this ILogger logger, BlobStorageResult result)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        var resultType = result.GetType().Name;
        _executingFileResultWithNoFileName(logger, resultType, result.FileDownloadName, null);
    }

    private static readonly Action<ILogger, string, string, Exception?> _executingFileResultWithNoFileName
        = LoggerMessage.Define<string, string>(
            eventId: new EventId(1, nameof(ExecutingBlobStorageResult)),
            logLevel: LogLevel.Information,
            formatString: "Executing {FileResultType}, sending file with download name '{FileDownloadName}'");

    private static readonly Action<ILogger, Exception?> _writingRangeToBody
        = LoggerMessage.Define(
            eventId: new EventId(17, nameof(WritingRangeToBody)),
            logLevel: LogLevel.Debug,
            formatString: "Writing the requested range of bytes to the body.");

    private static void WritingRangeToBody(this ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            _writingRangeToBody(logger, null);
        }
    }
}