using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class ValidatorExtension
    {
        public static bool ValidateControl<T>(this Control  container,
                                              T             instance,
                                              ErrorProvider errorProvider)
            where T : class, new()
        {
            ICollection<ValidationResult> validationResults = null;
            return ValidateControl(container, instance, errorProvider, out validationResults);
        }

        public static bool ValidateControl<T>(this Control                      container,
                                              T                                 instance,
                                              ErrorProvider                     errorProvider,
                                              out ICollection<ValidationResult> validationResults)
            where T : class, new()
        {
            var innerControls = new Dictionary<string, Control>();
            container.GetAllInnerControls<T>(ref innerControls);

            errorProvider.Clear();
            var validationContext = new ValidationContext(instance, null, null);
            validationResults = new List<ValidationResult>();
            var isValid =Validator.TryValidateObject(instance, validationContext,validationResults, true);
            if (isValid)
            {
                return isValid;
            }

            foreach (var validationResult in validationResults)
            {
                foreach (var member in validationResult.MemberNames)
                {
                    if (!innerControls.ContainsKey(member))
                    {
                        continue;
                    }

                    var control = innerControls[member];
                    errorProvider.SetError(control, validationResult.ErrorMessage);
                }
            }

            return isValid;
        }

        private static void FindControlIfEqualProperty<T>(Control                         innerControl,
                                                          ref Dictionary<string, Control> existControls)
        {
            var underLine     = '_';
            var propertyInfos = typeof(T).GetProperties();
            var columnName    = innerControl.Name.Split(underLine)[0];

            var findProperty = propertyInfos.FirstOrDefault(p => p.Name == columnName);
            var hasProperty  = findProperty != null;

            var isMatch = hasProperty && columnName == findProperty.Name;

            if (!isMatch)
            {
                return;
            }

            if (!existControls.ContainsKey(columnName))
            {
                existControls.Add(columnName, innerControl);
            }
        }

        private static void GetAllInnerControls<T>(this Control                     container,
                                                   ref  Dictionary<string, Control> existControls)
        {
            foreach (var innerControl in container.Controls.Cast<Control>())
            {
                if (innerControl.Controls.Count > 0)
                {
                    GetAllInnerControls<T>(innerControl, ref existControls);
                }

                FindControlIfEqualProperty<T>(innerControl, ref existControls);
            }
        }
    }
}