using System;
using System.ComponentModel.DataAnnotations;
using Nancy;
using Nancy.ModelBinding;

namespace Lab.ServiceNet4
{
    public class PersonNancyModule : NancyModule
    {
        public PersonNancyModule() : base("api/person")
        {
            this.Get[""]            = this.GetAllAction1;
            this.Get["{id:int}"]    = this.GetAction;
            this.Post[""]           = this.InsertAction1;
            this.Put["{id:int}"]    = this.EditAction;
            this.Delete["{id:int}"] = this.DeleteAction;
        }

        private dynamic DeleteAction(dynamic arg)
        {
            throw new NotImplementedException();
        }

        private dynamic EditAction(dynamic arg)
        {
            throw new NotImplementedException();
        }

        private dynamic GetAction(dynamic arg)
        {
            var id = arg.id != null ? (int) arg.id : default(int?);

            //TODO:Do thing
            return HttpStatusCode.OK;
        }

        private dynamic GetAllAction(dynamic arg)
        {
            var id   = this.Request.Query.id      != null ? (int) this.Request.Query.id : default(int?);
            var name = this.Request.Query["name"] != null ? this.Request.Query["name"].ToString() : null;

            //TODO:Do thing
            return new {id, name};
        }

        private dynamic GetAllAction1(dynamic arg)
        {
            var id   = this.Request.Form.id      != null ? (int) this.Request.Form.id : default(int?);
            var name = this.Request.Form["name"] != null ? this.Request.Form["name"].ToString() : null;

            //TODO:Do thing
            return new {id, name};
        }

        private dynamic InsertAction(dynamic parameters)
        {
            var source = this.Bind<Person>();

            //TODO:Insert data
            return source;
        }

        private dynamic InsertAction1(dynamic parameters)
        {
            var source = this.BindAndValidate<Person>();
            if (!this.ModelValidationResult.IsValid)
            {
                return this.ModelValidationResult;
            }

            //TODO:Insert data
            return HttpStatusCode.OK;
        }

        internal class Person
        {
            public Guid Id { get; set; }

            [Required]
            [StringLength(20)]
            public string Name { get; set; }

            public int SequenceId { get; set; }
        }
    }
}