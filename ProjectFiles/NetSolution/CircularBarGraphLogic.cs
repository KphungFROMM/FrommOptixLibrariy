#region Using directives
using FTOptix.NetLogic;
using FTOptix.UI;
using System.Xml.Linq;
using System.Linq;
using UAManagedCore;
using System;
#endregion

/// <summary>
/// This NetLogic should be placed as a CHILD of the AdvancedSVGImage object.
/// This makes the component self-contained and portable.
/// </summary>
public class CircularBarGraphLogic : BaseNetLogic
{
    private AdvancedSVGImage svgImage;
    private XDocument xDocument;
    private IUAVariable currentValueVariable;
    private IUAVariable minValueVariable;
    private IUAVariable maxValueVariable;
    private IUAVariable barWidthVariable;
    private IUAVariable barColorVariable;
    private IUAVariable backgroundColorVariable;
    
    // SVG Elements
    private XElement progressCircle;
    private XElement backgroundCircle;
    private XElement valueText;
    
    // Circle properties
    private double radius = 80;
    private double circumference;

    public override void Start()
    {
        // Owner is the AdvancedSVGImage itself
        svgImage = Owner as AdvancedSVGImage;
        if (svgImage == null)
        {
            Log.Error("CircularBarGraph", "Owner is not an AdvancedSVGImage. Place this NetLogic as a child of the AdvancedSVGImage.");
            return;
        }

        // Get configuration variables from the LogicObject
        currentValueVariable = LogicObject.GetVariable("CurrentValue");
        minValueVariable = LogicObject.GetVariable("MinValue");
        maxValueVariable = LogicObject.GetVariable("MaxValue");
        barWidthVariable = LogicObject.GetVariable("BarWidth");
        barColorVariable = LogicObject.GetVariable("BarColor");
        backgroundColorVariable = LogicObject.GetVariable("BackgroundColor");

        // Set default values if variables don't exist
        if (minValueVariable != null && minValueVariable.Value == null)
            minValueVariable.Value = 0;
        if (maxValueVariable != null && maxValueVariable.Value == null)
            maxValueVariable.Value = 100;
        if (barWidthVariable != null && barWidthVariable.Value == null)
            barWidthVariable.Value = 20;
        if (barColorVariable != null && barColorVariable.Value == null)
            barColorVariable.Value = 0xFF4CB050u; // Green (RGB: 76, 175, 80)
        if (backgroundColorVariable != null && backgroundColorVariable.Value == null)
            backgroundColorVariable.Value = 0xFFE0E0E0u; // Light gray (RGB: 224, 224, 224)

        // Load the SVG
        LoadSVG();
        
        // Initial update
        UpdateGraph();

        // Register for value changes
        if (currentValueVariable != null)
            currentValueVariable.VariableChange += OnValueChanged;
        if (barWidthVariable != null)
            barWidthVariable.VariableChange += OnBarWidthChanged;
        if (barColorVariable != null)
            barColorVariable.VariableChange += OnBarColorChanged;
        if (backgroundColorVariable != null)
            backgroundColorVariable.VariableChange += OnBackgroundColorChanged;
    }

    public override void Stop()
    {
        // Unregister events
        if (currentValueVariable != null)
            currentValueVariable.VariableChange -= OnValueChanged;
        if (barWidthVariable != null)
            barWidthVariable.VariableChange -= OnBarWidthChanged;
        if (barColorVariable != null)
            barColorVariable.VariableChange -= OnBarColorChanged;
        if (backgroundColorVariable != null)
            backgroundColorVariable.VariableChange -= OnBackgroundColorChanged;
    }

    private void LoadSVG()
    {
        try
        {
            // Get the SVG path
            var imageAbsolutePath = svgImage.Path.Uri;
            
            // Load the SVG into an XDocument
            xDocument = XDocument.Load(imageAbsolutePath);
            
            // Find SVG elements
            progressCircle = xDocument.Descendants()
                .Where(x => x.Name.LocalName == "circle" && x.Attribute("id")?.Value == "progress")
                .FirstOrDefault();
                
            backgroundCircle = xDocument.Descendants()
                .Where(x => x.Name.LocalName == "circle" && x.Attribute("id")?.Value == "background")
                .FirstOrDefault();
                
            valueText = xDocument.Descendants()
                .Where(x => x.Name.LocalName == "text" && x.Attribute("id")?.Value == "valueText")
                .FirstOrDefault();

            if (progressCircle == null || backgroundCircle == null || valueText == null)
            {
                Log.Error("CircularBarGraph", "Required SVG elements not found");
                return;
            }

            // Get radius from SVG
            var radiusAttr = progressCircle.Attribute("r");
            if (radiusAttr != null)
                radius = double.Parse(radiusAttr.Value);

            // Calculate circumference
            circumference = 2 * Math.PI * radius;
        }
        catch (Exception ex)
        {
            Log.Error("CircularBarGraph", $"Error loading SVG: {ex.Message}");
        }
    }

    private void OnValueChanged(object sender, VariableChangeEventArgs e)
    {
        UpdateGraph();
    }

    private void OnBarWidthChanged(object sender, VariableChangeEventArgs e)
    {
        UpdateBarWidth();
    }

    private void OnBarColorChanged(object sender, VariableChangeEventArgs e)
    {
        UpdateBarColor();
    }

    private void OnBackgroundColorChanged(object sender, VariableChangeEventArgs e)
    {
        UpdateBackgroundColor();
    }

    private void UpdateGraph()
    {
        if (xDocument == null || progressCircle == null || valueText == null)
            return;

        try
        {
            // Get values
            double currentValue = currentValueVariable != null ? currentValueVariable.Value : 0;
            double minValue = minValueVariable != null ? minValueVariable.Value : 0;
            double maxValue = maxValueVariable != null ? maxValueVariable.Value : 100;

            // Calculate percentage
            double percentage = 0;
            if (maxValue > minValue)
            {
                percentage = ((currentValue - minValue) / (maxValue - minValue)) * 100;
                percentage = Math.Max(0, Math.Min(100, percentage)); // Clamp between 0-100
            }

            // Calculate stroke-dashoffset for the progress circle
            double offset = circumference - (circumference * percentage / 100);

            // Update stroke-dashoffset
            var dashOffsetAttr = progressCircle.Attribute("stroke-dashoffset");
            if (dashOffsetAttr != null)
                dashOffsetAttr.Value = offset.ToString("F2");

            // Update text
            valueText.Value = Math.Round(percentage, 1).ToString("F1");

            // Apply the updated SVG
            svgImage.SetImageContent(xDocument.ToString());
        }
        catch (Exception ex)
        {
            Log.Error("CircularBarGraph", $"Error updating graph: {ex.Message}");
        }
    }

    private void UpdateBarWidth()
    {
        if (xDocument == null || progressCircle == null || backgroundCircle == null)
            return;

        try
        {
            double barWidth = barWidthVariable != null ? barWidthVariable.Value : 20;

            // Update both circles
            var progressWidthAttr = progressCircle.Attribute("stroke-width");
            if (progressWidthAttr != null)
                progressWidthAttr.Value = barWidth.ToString();

            var backgroundWidthAttr = backgroundCircle.Attribute("stroke-width");
            if (backgroundWidthAttr != null)
                backgroundWidthAttr.Value = barWidth.ToString();

            // Apply the updated SVG
            svgImage.SetImageContent(xDocument.ToString());
        }
        catch (Exception ex)
        {
            Log.Error("CircularBarGraph", $"Error updating bar width: {ex.Message}");
        }
    }

    private void UpdateBarColor()
    {
        if (xDocument == null || progressCircle == null)
            return;

        try
        {
            uint colorValue = (uint)(barColorVariable != null ? barColorVariable.Value : 0xFF4CB050u);
            string hexColor = ColorToHex(colorValue);

            // Update progress circle color
            var strokeAttr = progressCircle.Attribute("stroke");
            if (strokeAttr != null)
                strokeAttr.Value = hexColor;

            // Apply the updated SVG
            svgImage.SetImageContent(xDocument.ToString());
        }
        catch (Exception ex)
        {
            Log.Error("CircularBarGraph", $"Error updating bar color: {ex.Message}");
        }
    }

    private void UpdateBackgroundColor()
    {
        if (xDocument == null || backgroundCircle == null)
            return;

        try
        {
            uint colorValue = (uint)(backgroundColorVariable != null ? backgroundColorVariable.Value : 0xFFE0E0E0u);
            string hexColor = ColorToHex(colorValue);

            // Update background circle color
            var strokeAttr = backgroundCircle.Attribute("stroke");
            if (strokeAttr != null)
                strokeAttr.Value = hexColor;

            // Apply the updated SVG
            svgImage.SetImageContent(xDocument.ToString());
        }
        catch (Exception ex)
        {
            Log.Error("CircularBarGraph", $"Error updating background color: {ex.Message}");
        }
    }

    [ExportMethod]
    public void SetValue(double value)
    {
        if (currentValueVariable != null)
            currentValueVariable.Value = value;
    }

    [ExportMethod]
    public void SetRange(double min, double max)
    {
        if (minValueVariable != null)
            minValueVariable.Value = min;
        if (maxValueVariable != null)
            maxValueVariable.Value = max;
        UpdateGraph();
    }

    [ExportMethod]
    public void SetBarWidth(double width)
    {
        if (barWidthVariable != null)
            barWidthVariable.Value = width;
    }

    [ExportMethod]
    public void SetColors(uint barColor, uint backgroundColor)
    {
        if (barColorVariable != null)
            barColorVariable.Value = barColor;
        if (backgroundColorVariable != null)
            backgroundColorVariable.Value = backgroundColor;
    }

    // Helper method to convert Color to hex string
    private string ColorToHex(uint color)
    {
        // Extract RGB components from uint (ARGB format)
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);
        return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
    }
}
