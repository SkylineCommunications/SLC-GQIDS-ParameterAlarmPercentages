/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

11/08/2023	1.0.0.1		Sebastiaan, Skyline	Initial version
****************************************************************************
*/

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
    private Task<ReportStateDataResponseMessage> _result;

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

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        if (_result == null)
            throw new GenIfException($"No alarm states requested for parameter {_parameter}.");

        ReportStateDataResponseMessage states = null;
        try
        {
            _result.Wait();
            states = _result.Result;
        }
        catch (AggregateException ex)
        {
            // Uncomment if you want to see detailed error message
            // throw new GenIfException(ex.InnerException.Message);
        }

        if (states == null)
            throw new GenIfException($"No alarm states could be retrieved for parameter {_parameter}.");

        return new GQIPage(new[]
        {
            FormatSeverityRow("Normal", states.PercentageNormal),
            FormatSeverityRow("Warning", states.PercentageWarning),
            FormatSeverityRow("Minor", states.PercentageMinor),
            FormatSeverityRow("Major", states.PercentageMajor),
            FormatSeverityRow("Critical", states.PercentageCritical),
            FormatSeverityRow("Masked", states.PercentageMasked),
            FormatSeverityRow("No templates", states.PercentageNoTemplate),
            FormatSeverityRow("Timeout", states.PercentageTimeout),
            FormatSeverityRow("Unknown", states.PercentageUnknown),
        } )
        {
            HasNextPage = false,
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

        _result = Task.Factory.StartNew<ReportStateDataResponseMessage>(() =>
        {
            var msg = new GetReportStateDataMessage()
            {
                ParameterID = _parameter.ParameterID_,
                Filter = ReportFilterInfo.Element(_parameter.DataMinerID, _parameter.ElementID),
                Timespan = DMADateTimeToStringFormat(start) + "|" + DMADateTimeToStringFormat(end),
            };

            return _callback.SendMessage(msg) as ReportStateDataResponseMessage;
        });

        return new OnPrepareFetchOutputArgs();
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