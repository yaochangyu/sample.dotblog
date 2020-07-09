using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Diagnostics;
using Microsoft.Owin.Diagnostics.Views;
using Microsoft.Owin.Logging;

namespace OWinHost.AspNet48.WebApi
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    ///     Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
    /// </summary>
    /// <remarks>copy from https://github.com/aspnet/AspNetKatana/blob/e2b18ec84ceab7ffa29d80d89429c9988ab40144/src/Microsoft.Owin.Diagnostics/Views/ErrorPage.cs</remarks>
    public class CustomErrorPageMiddleware
    {
        private readonly ILogger          _logger;
        private readonly AppFunc          _next;
        private readonly ErrorPageOptions _options;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomErrorPageMiddleware" /> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="isDevMode"></param>
        public CustomErrorPageMiddleware(AppFunc next, ErrorPageOptions options, ILogger logger, bool isDevMode)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (isDevMode)
            {
                options.SetDefaultVisibility(true);
            }

            this._next    = next;
            this._options = options;
            this._logger  = logger;
        }

        /// <summary>
        ///     Process an individual request.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
                         Justification = "For diagnostics")]
        public async Task Invoke(IDictionary<string, object> environment)
        {
            try
            {
                await this._next(environment);
            }
            catch (Exception ex)
            {
                try
                {
                    //this.LogException(ex);
                    this.DisplayException(new OwinContext(environment), ex);
                    return;
                }
                catch (Exception)
                {
                    // If there's a Exception while generating the error page, re-throw the original exception.
                }

                throw;
            }
        }

        // Assumes the response headers have not been sent.  If they have, still attempt to write to the body.
        private void DisplayException(IOwinContext context, Exception ex)
        {
            var request = context.Request;

            var model = new ErrorPageModel
            {
                Options = this._options,
            };

            if (this._options.ShowExceptionDetails)
            {
                model.ErrorDetails = this.GetErrorDetails(ex, this._options.ShowSourceCode).Reverse();

                //model.ErrorDetails = new ErrorDetails[] { };
            }

            if (this._options.ShowQuery)
            {
                model.Query = request.Query;
            }

            if (this._options.ShowCookies)
            {
                model.Cookies = request.Cookies;
            }

            if (this._options.ShowHeaders)
            {
                model.Headers = request.Headers;
            }

            if (this._options.ShowEnvironment)
            {
                model.Environment = request.Environment;
            }

            var errorPage = new ErrorPage {Model = model};
            errorPage.Execute(context);
        }

        private IEnumerable<ErrorDetails> GetErrorDetails(Exception ex, bool showSource)
        {
            for (var scan = ex; scan != null; scan = scan.InnerException)
            {
                yield return new ErrorDetails
                {
                    Error       = scan,
                    StackFrames = this.StackFrames(scan, showSource)
                };
            }
        }

        private StackFrame LoadFrame(string function, string file, int lineNumber, bool showSource)
        {
            var frame = new StackFrame {Function = function, File = file, Line = lineNumber};
            if (showSource && File.Exists(file))
            {
                var code = File.ReadAllLines(file);
                frame.PreContextLine = Math.Max(lineNumber - this._options.SourceCodeLineCount, 1);
                frame.PreContextCode = code.Skip(frame.PreContextLine - 1).Take(lineNumber - frame.PreContextLine)
                                           .ToArray();
                frame.ContextCode     = code.Skip(lineNumber - 1).FirstOrDefault();
                frame.PostContextCode = code.Skip(lineNumber).Take(this._options.SourceCodeLineCount).ToArray();
            }

            return frame;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
                         MessageId =
                             "Microsoft.Owin.Logging.LoggerExtensions.WriteError(Microsoft.Owin.Logging.ILogger,System.String,System.Exception)",
                         Justification = "We do not LOC logging messages.")]
        private void LogException(Exception ex)
        {
            if (this._logger != null)
            {
                this._logger.WriteError("The error page caught the following Exception:", ex);
            }
        }

        private StackFrame StackFrame(Chunk line, bool showSource)
        {
            line.Advance("  at ");
            var function   = line.Advance(" in ").ToString();
            var file       = line.Advance(":line ").ToString();
            var lineNumber = line.ToInt32();

            return string.IsNullOrEmpty(file)
                       ? this.LoadFrame(string.IsNullOrEmpty(function) ? line.ToString() : function, string.Empty, 0,
                                        showSource)
                       : this.LoadFrame(function, file, lineNumber, showSource);
        }

        private IEnumerable<StackFrame> StackFrames(Exception ex, bool showSource)
        {
            var stackTrace = ex.StackTrace;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var heap = new Chunk
                {
                    Text = stackTrace        + Environment.NewLine,
                    End  = stackTrace.Length + Environment.NewLine.Length
                };
                for (var line = heap.Advance(Environment.NewLine);
                     line.HasValue;
                     line = heap.Advance(Environment.NewLine))
                {
                    yield return this.StackFrame(line, showSource);
                }
            }
        }

        internal class Chunk
        {
            public string Text { get; set; }

            public int Start { get; set; }

            public int End { get; set; }

            public bool HasValue => this.Text != null;

            public Chunk Advance(string delimiter)
            {
                var indexOf = this.HasValue
                                  ? this.Text.IndexOf(delimiter, this.Start, this.End - this.Start,
                                                      StringComparison.Ordinal)
                                  : -1;
                if (indexOf < 0)
                {
                    return new Chunk();
                }

                var chunk = new Chunk {Text = this.Text, Start = this.Start, End = indexOf};
                this.Start = indexOf + delimiter.Length;
                return chunk;
            }

            public int ToInt32()
            {
                int value;
                return this.HasValue && int.TryParse(
                                                     this.Text.Substring(this.Start, this.End - this.Start),
                                                     NumberStyles.Integer,
                                                     CultureInfo.InvariantCulture,
                                                     out value)
                           ? value
                           : 0;
            }

            public override string ToString()
            {
                return this.HasValue ? this.Text.Substring(this.Start, this.End - this.Start) : string.Empty;
            }
        }
    }
}