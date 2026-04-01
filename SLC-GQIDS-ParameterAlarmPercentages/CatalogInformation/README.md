# Parameter Alarm Percentages

## About

The **Parameter Alarm Percentages** data source retrieves the alarm severity percentages of a parameter within a specified time period, and presents this information in a table with 2 columns:

- **Severity**: A column of type string, of which each row contains an alarm severity.
- **Percentage**: A column of type double, of which each row contains the percentage of the time the parameter had the alarm severity in question.

The data can be visualized in any GQI visualization. A pie chart is most suitable for this type of data.

![Severities example](./Images/Severities.png)

![Piechart example](./Images/PieChart.png)

> [!NOTE]
> When a pie chart is used, the colors need to be configured manually. These colors are mapped based on the row order.

## Key Features

- Shows the percentage of time a parameter has spent in each alarm severity
- Can be used for any standalone parameter

## Use Cases

### Monitoring Parameter Alarm Distribution

Visualize the distribution of alarm severities for a specific parameter over a given time window to identify patterns and trends in alarm behavior.

### Time-based Alarm Analysis

Analyze how much time a parameter has spent in the different severity states (Normal, Warning, Minor, Major, Critical) to assess system health and performance.

## Configuration

This data source requires 3 arguments:

- **Parameter Id**: Format: "A/B/C"

  - A = DataMiner ID
  - B = Element ID
  - C = Parameter ID

- **Start of the time window**: The timestamp marking the beginning of the time window.
- **End of the time window**: The timestamp marking the end of the time window.

![Arguments](./Images/Arguments.png)
