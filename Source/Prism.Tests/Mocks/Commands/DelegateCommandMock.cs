using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prism.Tests.Mocks.Commands
{
    public class DelegateCommandMock : DelegateCommandBase
    {
        public DelegateCommandMock(Action<object> executeMethod) :
            base(executeMethod, (o) => true)
        {

        }

        public DelegateCommandMock(Action<object> executeMethod, Func<object, bool> canExecuteMethod) :
            base(executeMethod, canExecuteMethod)
        {

        }
    }
}
