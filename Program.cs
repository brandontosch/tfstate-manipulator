using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using tfstate_manipulator.Data;
using tfstate_manipulator.Models;

namespace tfstate_manipulator
{
    static class Program
    {
        static void Main(string[] args)
        {
            var stateFile = args[0];
            var stateReader = new StateReader();
            JObject stateData = stateReader.GetStateData(stateFile);

            var modules = (JArray)stateData["modules"];
            var vms = new List<JObject>();

            foreach (var module in modules)
            {
                var resources = (JObject)module["resources"];
                var vmResources = resources.Properties()
                    .Where(property => property.Value.Value<string>("type") == "azurerm_virtual_machine")
                    .Select(property => property.Value.Value<JObject>("primary"))
                    .Cast<JObject>().ToArray();
                vms.AddRange(vmResources);
            }

            foreach (var vm in vms)
            {
                var attributes = (JObject)vm["attributes"];
                VMTypes vmType = GetVMType(attributes);
                var attribsToRemove = new List<JProperty>();

                switch (vmType)
                {
                    case VMTypes.NoId2012:
                        {
                            attribsToRemove.AddRange(attributes.Properties().Where(attrib => attrib.Name.StartsWith("storage_image_reference.522281604.")));
                            attributes.AddNewPropertyAfter("id", "license_type", "Windows_Server");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.2991307580.version", "latest");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.2991307580.sku", "2012-R2-Datacenter");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.2991307580.publisher", "MicrosoftWindowsServer");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.2991307580.offer", "WindowsServer");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.2991307580.id", "");
                            break;
                        }
                    case VMTypes.NoId2016:
                        {
                            attribsToRemove.AddRange(attributes.Properties().Where(attrib => attrib.Name.StartsWith("storage_image_reference.3349291985.")));
                            attributes.AddNewPropertyAfter("id", "license_type", "Windows_Server");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.version", "latest");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.sku", "2016-Datacenter");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.publisher", "MicrosoftWindowsServer");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.offer", "WindowsServer");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.id", "");
                            break;
                        }
                    case VMTypes.WithId2016:
                        {
                            attribsToRemove.AddRange(attributes.Properties().Where(attrib => attrib.Name.StartsWith("storage_image_reference.1724838809.")));
                            attributes.AddNewPropertyAfter("id", "license_type", "Windows_Server");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.version", "latest");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.sku", "2016-Datacenter");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.publisher", "MicrosoftWindowsServer");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.offer", "WindowsServer");
                            attributes.AddNewPropertyAfter("storage_image_reference.#", "storage_image_reference.3904372903.id", "");
                            break;
                        }
                }

                foreach(var attrib in attribsToRemove)
                {
                    attrib.Remove();
                }
            }

            using (StreamWriter newStateFile = File.CreateText(stateFile + ".new"))
            {
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(newStateFile))
                {
                    stateData.WriteTo(jsonTextWriter);
                }
            }
        }

        public static void AddNewPropertyAfter(this JObject jObject, string existingName, string newName, string value)
        {
            var newProperty = JObject.Parse(string.Format("{{\"{0}\":\"{1}\"}}", newName, value)).Properties().First();
            var existingProperty = jObject.Property(existingName);
            existingProperty.AddAfterSelf(newProperty);
        }

        private static VMTypes GetVMType(JObject attributes)
        {
            if (attributes.GetValue("storage_image_reference.1724838809.offer") != null)
            {
                return VMTypes.WithId2016;
            }
            else if (attributes.GetValue("storage_image_reference.522281604.offer") != null)
            {
                return VMTypes.NoId2012;
            }
            else if (attributes.GetValue("storage_image_reference.3349291985.offer") != null)
            {
                return VMTypes.NoId2016;
            }

            return VMTypes.Other;
        }
    }
}
