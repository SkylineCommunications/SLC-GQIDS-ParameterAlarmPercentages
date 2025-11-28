using System;
using System.Globalization;
using System.Threading.Tasks;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;

[GQIMetaData(Name = "Parameter alarm percentages")]
public class CSVDataSource : IGQIOnInit, IGQIDataSource, IGQIInputArguments, IGQIOnPrepareFetch
{
	private static GQIStringColumn _severityColumn = new GQIStringColumn("Severity");
	private static GQIDoubleColumn _percentageColumn = new GQIDoubleColumn("Percentage");

	private static GQIStringArgument _paramIdArg = new GQIStringArgument("Parameter Id") { IsRequired = true };
	private static GQIDateTimeArgument _startArg = new GQIDateTimeArgument("Start") { IsRequired = false };
	private static GQIDateTimeArgument _endArg = new GQIDateTimeArgument("End") { IsRequired = false };

	private ParameterID _parameter;
	private DateTime _start;
	private DateTime _end;
	private GQIDMS _callback;
	private ReportStateDataResponseMessage _states;

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[]
		{
			_paramIdArg,
			_startArg,
			_endArg,
		};
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		var paramID = args.GetArgumentValue(_paramIdArg);
		var result = new OnArgumentsProcessedOutputArgs();
		if (string.IsNullOrWhiteSpace(paramID))
			return result;

		var parts = paramID.Split('/');
		if (parts.Length != 3)
			return result;

		if (int.TryParse(parts[0], out var dmaID) &&
			int.TryParse(parts[1], out var elementID) &&
			int.TryParse(parts[2], out var parameterID))
			_parameter = new ParameterID(dmaID, elementID, parameterID);

		args.TryGetArgumentValue(_startArg, out _start);
		args.TryGetArgumentValue(_endArg, out _end);

		return result;
	}

	public GQIColumn[] GetColumns()
	{
		return new GQIColumn[]
		{
			_severityColumn,
			_percentageColumn,
		};
	}

	public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
	{
		if (_parameter == null)
			throw new GenIfException("Invalid parameter ID.");

		DateTime start;
		DateTime end;
		if (_start != DateTime.MinValue && _end != DateTime.MinValue)
		{
			start = _start;
			end = _end;
		}
		else
		{
			var now = DateTime.UtcNow;
			start = DateTime.UtcNow - TimeSpan.FromHours(24);
			end = now;
		}

		var msg = new GetReportStateDataMessage()
		{
			ParameterID = _parameter.ParameterID_,
			Filter = ReportFilterInfo.Element(_parameter.DataMinerID, _parameter.ElementID),
			Timespan = DMADateTimeToStringFormat(start) + "|" + DMADateTimeToStringFormat(end),
		};

		_states = _callback.SendMessage(msg) as ReportStateDataResponseMessage;

		return new OnPrepareFetchOutputArgs();
	}

	public GQIPage GetNextPage(GetNextPageInputArgs args)
	{
		if (_states == null)
			throw new GenIfException($"No alarm states requested for parameter {_parameter}.");

		if (_states == null)
			throw new GenIfException($"No alarm states could be retrieved for parameter {_parameter}.");

		return new GQIPage(new[]
		{
			FormatSeverityRow("Normal", _states.PercentageNormal),
			FormatSeverityRow("Warning", _states.PercentageWarning),
			FormatSeverityRow("Minor", _states.PercentageMinor),
			FormatSeverityRow("Major", _states.PercentageMajor),
			FormatSeverityRow("Critical", _states.PercentageCritical),
			FormatSeverityRow("Masked", _states.PercentageMasked),
			FormatSeverityRow("No templates", _states.PercentageNoTemplate),
			FormatSeverityRow("Timeout", _states.PercentageTimeout),
			FormatSeverityRow("Unknown", _states.PercentageUnknown),
		})
		{
			HasNextPage = false,
		};
	}

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_callback = args.DMS;
		return new OnInitOutputArgs();
	}

	private static string DMADateTimeToStringFormat(DateTime input)
	{
		return input.ToString("yyyy'-'MM'-'dd HH':'mm':'ss", CultureInfo.InvariantCulture);
	}

	private static GQIRow FormatSeverityRow(string severity, double percentage)
	{
		return new GQIRow(new[]
		{
			new GQICell() {Value = severity },
			FormatPercentageCell(percentage),
		});
	}

	private static GQICell FormatPercentageCell(double percentage)
	{
		return new GQICell() { Value = percentage, DisplayValue = $"{Math.Round(percentage, 2)} %" };
	}
}