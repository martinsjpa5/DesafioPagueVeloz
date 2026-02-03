using Moq.AutoMock;

namespace Tests
{
    public class BaseTest
    {
        public readonly AutoMocker _autoMocker;
        public BaseTest()
        {
            _autoMocker = new AutoMocker();
        }
    }
}

