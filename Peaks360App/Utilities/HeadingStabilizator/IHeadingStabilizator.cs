namespace Peaks360App.Utilities.HeadingStabilizator
{
    public interface IHeadingStabilizator
    {
        public void AddValue(double value);
        public double GetHeading();
    }
}