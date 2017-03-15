﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    public abstract class AbstractTemplateSelector : AbstractMemberBinding
    {
        /// <summary>
        /// The view-model, cached during connection.
        /// </summary>
        protected object viewModel;

        /// <summary>
        /// The name of the property we are binding to on the view model.
        /// </summary>
        public string viewModelPropertyName = string.Empty;

        /// <summary>
        /// Watches the view-model property for changes.
        /// </summary>
        protected PropertyWatcher viewModelPropertyWatcher;

        /// <summary>
        /// The gameobject in the scene that is the parent object for the tenplates.
        /// </summary>
        public GameObject templatesRoot;

        /// <summary>
        /// All available templates indexed by the view model the are for.
        /// </summary>
        private readonly IDictionary<Type, Template> availableTemplates = new Dictionary<Type, Template>();

        /// <summary>
        /// All the child objects that have been created, indexed by the view they are connected to.
        /// </summary>
        private readonly IDictionary<object, GameObject> instantiatedTemplates = new Dictionary<object, GameObject>();

        protected new void Awake()
        {
            Assert.IsNotNull(templatesRoot, "No templates have been assigned.");

            CacheTemplates();

            base.Awake();
        }

        // Cache available templates.
        private void CacheTemplates()
        {
            availableTemplates.Clear();

            var templatesInScene = templatesRoot.GetComponentsInChildren<Template>(true);
            foreach (var template in templatesInScene)
            {
                template.gameObject.SetActive(false);
                var typeName = template.GetViewModelTypeName();
                var type = TypeResolver.TypesWithBindingAttribute.FirstOrDefault(t => t.ToString() == typeName);
                if (type == null)
                {
                    Debug.LogError(string.Format("Template object {0} references type {1}, but no matching type with a [Binding] attribute could be found.", template.name, typeName), template);
                    continue;
                }
                
                availableTemplates.Add(type, template);
            }
        }

        /// <summary>
        /// Create a clone of the template object and bind it to the specified view model.
        /// </summary>
        protected void InstantiateTemplate(object templateViewModel)
        {
            Assert.IsNotNull(templateViewModel, "Cannot instantiate child with null view model");
            
            // Select template.
            var viewModelType = templateViewModel.GetType();
            Template selectedTemplate;
            if (!availableTemplates.TryGetValue(viewModelType, out selectedTemplate))
            {
                throw new ApplicationException("Cannot find matching template for: " + viewModelType);
            }

            var newObject = Instantiate(selectedTemplate);
            newObject.transform.SetParent(transform, false);

            instantiatedTemplates.Add(templateViewModel, newObject.gameObject);

            // Set up child bindings before we activate the template object so that they will be configured properly before trying to connect.
            newObject.InitChildBindings(templateViewModel);

            newObject.gameObject.SetActive(true);
        }

        /// <summary>
        /// Destroys the instantiated template associated with the provided object.
        /// </summary>
        protected void DestroyTemplate(object viewModelToDestroy)
        {
            Destroy(instantiatedTemplates[viewModelToDestroy]);
            instantiatedTemplates.Remove(viewModelToDestroy);
        }

        /// <summary>
        /// Destroys all instantiated templates.
        /// </summary>
        protected void DestroyAllTemplates()
        {
            foreach (var generatedChild in instantiatedTemplates.Values)
            {
                Destroy(generatedChild);
            }

            instantiatedTemplates.Clear();
        }
    }
}
