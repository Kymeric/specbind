﻿// <copyright file="PageBuilderBase.cs">
//    Copyright © 2013 Dan Piessens.  All rights reserved.
// </copyright>
namespace SpecBind.Pages
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using SpecBind.BrowserSupport;
    using SpecBind.Helpers;

    /// <summary>
    /// A base class for creating page models for the control.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent control that should be injected into the page.</typeparam>
    /// <typeparam name="TOutput">The type of the created output control.</typeparam>
    /// <typeparam name="TElement">The type of the element control used for a property.</typeparam>
    public abstract class PageBuilderBase<TParent, TOutput, TElement>
        where TOutput : class
    {
        private readonly MethodInfo assignMethodInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageBuilderBase{TParent, TOutput, TElement}"/> class.
        /// </summary>
        protected PageBuilderBase()
        {
            this.assignMethodInfo = new Action<TElement, ElementLocatorAttribute, object[]>(this.AssignElementAttributes).GetMethodInfo();
        }

        /// <summary>
        /// Gets a value indicating whether to allow an empty constructor for a page object.
        /// </summary>
        /// <value><c>true</c> if an empty constructor should be allowed; otherwise, <c>false</c>.</value>
        protected virtual bool AllowEmptyConstructor
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Assigns the element attributes.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="attribute">The attribute.</param>
        /// <param name="nativeAttributes">The native attributes.</param>
        protected abstract void AssignElementAttributes(TElement control, ElementLocatorAttribute attribute, object[] nativeAttributes);

        /// <summary>
        /// Assigns the page element attributes.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="locatorAttribute">The locator attribute.</param>
        protected virtual void AssignPageElementAttributes(TOutput control, ElementLocatorAttribute locatorAttribute)
        {

        }

        /// <summary>
        /// Gets the type of the element collection.
        /// </summary>
        /// <returns>A type that implements IElementList.</returns>
        protected abstract Type GetElementCollectionType();

        /// <summary>
        /// Gets the custom driver specific attributes for a given type.
        /// </summary>
        /// <param name="propertyInfo">Type of the item.</param>
        /// <returns>The collection of custom attributes.</returns>
        protected virtual object[] GetCustomAttributes(PropertyInfo propertyInfo)
        {
            return new object[0];
        }

        /// <summary>
        /// Checks to see if the control is the same type as the base class and performs the appropriate actions.
        /// </summary>
        /// <param name="control">The control.</param>
        protected virtual void CheckPageIsBaseClass(TOutput control)
        {
        }

        /// <summary>
        /// Creates the page or wrapper element control.
        /// </summary>
        /// <param name="elementType">Type of the page.</param>
        /// <returns>The page builder function.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the constructor is invalid.</exception>
        protected Func<TParent, IBrowser, Action<TOutput>, TOutput> CreateElementInternal(Type elementType)
        {
            var expression = this.CreateNewItemExpression(elementType);
            return expression.Compile();
        }

        /// <summary>
        /// Creates the frame locator method to help load that from a property.
        /// </summary>
        /// <param name="frameType">Type of the class that will provide the frame.</param>
        /// <param name="property">The property on the class that should be accessed to provide the frame.</param>
        /// <returns>The function used to create the document.</returns>
        protected Func<TParent, TOutput> CreateFrameLocatorInternal(Type frameType, PropertyInfo property)
        {
            var createExpression = this.CreateNewItemExpression(frameType);

            var parentArgument = Expression.Parameter(typeof(TParent), "parent");
            var docVariable = Expression.Variable(frameType);

            var expressions = new List<Expression>
				                  {
					                  Expression.Assign(docVariable, Expression.Convert(Expression.Invoke(createExpression, parentArgument, Expression.Constant(null, typeof(IBrowser)), Expression.Constant(null, typeof(Action<TElement>))), frameType)),
									  Expression.Convert(Expression.Property(docVariable, property), typeof(TOutput))
				                  };

            var methodCall = Expression.Block(new[] { docVariable }, expressions);

            return Expression.Lambda<Func<TParent, TOutput>>(methodCall, parentArgument).Compile();
        }

        /// <summary>
        /// Creates the constructor exception to be thrown if the type cannot be created.
        /// </summary>
        /// <param name="propertyName">Name of the property being created; otherwise <c>null</c>.</param>
        /// <param name="expectedControlType">Expected type of the control.</param>
        /// <returns>The exception that will be thrown to the user.</returns>
        protected virtual Exception CreateConstructorException(string propertyName, Type expectedControlType)
        {
            string message;
            if (!string.IsNullOrEmpty(propertyName))
            {

                message =
                    string.Format(
                        "Property '{0}' of type '{1}' has an invalid constructor. Elements need to inherit the base constructor that accepts a {2} parameter.",
                        propertyName,
                        expectedControlType.Name,
                        typeof(TParent).Name);
            }
            else
            {
                message = string.Format(
                    "Constructor on type '{0}' must have a single argument of type {1}.",
                    expectedControlType.Name,
                    typeof(TParent).Name);
            }

            return new InvalidOperationException(message);
        }

        /// <summary>
        /// Gets the constructor parameter for the given type.
        /// </summary>
        /// <param name="parameterType">Type of the parameter to fill.</param>
        /// <param name="parentArgument">The parent argument.</param>
        /// <param name="rootLocator">The root locator argument if different from the parent.</param>
        /// <returns>The constructor information that matches.</returns>
        protected virtual Expression FillConstructorParameter(Type parameterType, ExpressionData parentArgument, ExpressionData rootLocator)
        {
            return typeof(TParent).IsAssignableFrom(parameterType) ? parentArgument.Expression : null;
        }

        /// <summary>
        /// Gets the type of the property proxy.
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        /// <returns>The property type.</returns>
        protected virtual Type GetPropertyProxyType(Type propertyType)
        {
            return propertyType;
        }

        /// <summary>
        /// Checks to see if the control is the same type as the base class and performs the appropriate actions.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="attribute">The page navigation attribute.</param>
        protected virtual void SetPageNavigationAttribute(TOutput control, PageNavigationAttribute attribute)
        {
        }

        /// <summary>
        /// Assigns the attributes for the document based on its metadata.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="customAction">The custom action.</param>
        private void AssignPageAttributes(TOutput control, Action<TOutput> customAction)
        {
            this.CheckPageIsBaseClass(control);

            if (customAction != null)
            {
                customAction.Invoke(control);
            }

            var controlType = control.GetType();
            PageNavigationAttribute attribute;
            if (controlType.TryGetAttribute(out attribute))
            {
                this.SetPageNavigationAttribute(control, attribute);
            }

            ElementLocatorAttribute locatorAttribute;
            if (controlType.TryGetAttribute(out locatorAttribute))
            {
                this.AssignPageElementAttributes(control, locatorAttribute);
            }
        }

        /// <summary>
        /// Creates the new item expression that creates the object and initial mapping.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns>The initial creation lambda expression.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the constructor is invalid.</exception>
        private Expression<Func<TParent, IBrowser, Action<TOutput>, TOutput>> CreateNewItemExpression(Type elementType)
        {
            var parentParameter = Expression.Parameter(typeof(TParent), "parent");
            var parentArgument = new ExpressionData(parentParameter, typeof(TParent));

            var browserParameter = Expression.Parameter(typeof(IBrowser), "browser");
            var browserArgument = new ExpressionData(browserParameter, typeof(IBrowser));

            var constructor = this.GetConstructor(elementType, browserArgument, parentArgument, null);
            if (constructor == null)
            {
                throw this.CreateConstructorException(null, elementType);
            }

            var actionParameter = Expression.Parameter(typeof(Action<TOutput>), "action");
            var docVariable = Expression.Variable(elementType);

            //Spin though properties and make an initializer for anything we can set that has an attribute
            var pageMethodInfo = new Action<TOutput, Action<TOutput>>(this.AssignPageAttributes).GetMethodInfo();
            var expressions = new List<Expression>
				                  {
					                  Expression.Assign(docVariable, Expression.New(constructor.Item1, constructor.Item2)),
					                  Expression.Call(
                                          Expression.Constant(this),
						                  pageMethodInfo,
						                  Expression.Convert(docVariable, typeof(TOutput)),
						                  actionParameter)
				                  };

            var documentData = new ExpressionData(docVariable, elementType);
            this.MapObjectProperties(expressions, elementType, browserArgument, documentData, parentArgument);
            expressions.Add(docVariable);

            var methodCall = Expression.Block(new[] { docVariable }, expressions);
            return Expression.Lambda<Func<TParent, IBrowser, Action<TOutput>, TOutput>>(methodCall, parentParameter, browserParameter, actionParameter);
        }

        /// <summary>
        /// Gets the constructor.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="browser">The browser argument to inject a browser.</param>
        /// <param name="parentArgument">The parent argument.</param>
        /// <param name="rootLocator">The root locator argument if different from the parent.</param>
        /// <returns>The constructor information that matches.</returns>
        private Tuple<ConstructorInfo, IEnumerable<Expression>> GetConstructor(Type itemType, ExpressionData browser, ExpressionData parentArgument, ExpressionData rootLocator)
        {
            ConstructorInfo emptyConstructor = null;
            foreach (var constructorInfo in itemType.GetConstructors(BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                    .OrderByDescending(c => c.GetParameters().Length))
            {

                var parameters = constructorInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    emptyConstructor = constructorInfo;
                    continue;
                }

                var slots = new Expression[parameters.Length];
                slots.Initialize();

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterType = parameter.ParameterType;

                    if (browser.Type.IsAssignableFrom(parameterType))
                    {
                        slots[i] = Expression.Convert(browser.Expression, browser.Type);
                    }
                    else
                    {
                        slots[i] = this.FillConstructorParameter(parameterType, parentArgument, rootLocator);
                    }
                }

                if (slots.All(s => s != null))
                {
                    return new Tuple<ConstructorInfo, IEnumerable<Expression>>(constructorInfo, slots);
                }
            }

            return this.AllowEmptyConstructor && emptyConstructor != null
                       ? new Tuple<ConstructorInfo, IEnumerable<Expression>>(emptyConstructor, new List<Expression>(0))
                       : null;
        }
        

        /// <summary>
        /// Maps the object properties for the given object.
        /// </summary>
        /// <param name="expressions">The parent expression list.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="browser">The browser argument.</param>
        /// <param name="parentVariable">The parent variable.</param>
        /// <param name="rootLocator">The root locator item.</param>
        private void MapObjectProperties(ICollection<Expression> expressions, Type objectType, ExpressionData browser, ExpressionData parentVariable, ExpressionData rootLocator)
        {
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var propertyInfo in objectType.GetProperties().Where(p =>
                p.SetMethod != null && p.CanWrite && (typeof(TElement).IsAssignableFrom(p.PropertyType) || p.PropertyType.IsElementListType())))
            // ReSharper restore LoopCanBeConvertedToQuery
            {
                var propertyType = propertyInfo.PropertyType;
                var attribute = propertyInfo.GetCustomAttributes(typeof(ElementLocatorAttribute), false).FirstOrDefault() as ElementLocatorAttribute;
                var customAttributes = this.GetCustomAttributes(propertyInfo);
                if (attribute == null && customAttributes.Length == 0)
                {
                    continue;
                }

                //Final Properties
                var itemVariable = Expression.Variable(propertyType);
                var variableList = new List<ParameterExpression> { itemVariable };
                var propertyExpressions = new List<Expression>();

                //Special case for lists
                if (propertyType.IsElementListType())
                {
                    var concreteType = this.GetElementCollectionType().MakeGenericType(propertyType.GetGenericArguments());
                    var concreteTypeConstructor = concreteType.GetConstructors().First();

                    var parentListType = propertyType.GetGenericArguments().First();
                    var parentListVariable = Expression.Variable(parentListType, "collectionParent");
                    variableList.Add(parentListVariable);

                    propertyExpressions.AddRange(this.CreateHtmlObject(browser, rootLocator, parentVariable, parentListVariable, parentListType, propertyInfo.Name, attribute, customAttributes));
                    propertyExpressions.Add(Expression.Assign(itemVariable, Expression.New(concreteTypeConstructor, parentListVariable, browser.Expression)));
                }
                else
                {
                    //Normal path starts here
                    //New up property and then check if for inner properties.
                    propertyExpressions.AddRange(this.CreateHtmlObject(browser, rootLocator, parentVariable, itemVariable, propertyType, propertyInfo.Name, attribute, customAttributes));

                    var itemData = new ExpressionData(itemVariable, propertyType);
                    this.MapObjectProperties(propertyExpressions, propertyType, browser, itemData, rootLocator);
                }

                propertyExpressions.Add(Expression.Assign(Expression.Property(parentVariable.Expression, propertyInfo), itemVariable));
                expressions.Add(Expression.Block(variableList, propertyExpressions));
            }
        }

        /// <summary>
        /// Creates the HTML object for each property that is part of the parent.
        /// </summary>
        /// <param name="browser">The browser argument.</param>
        /// <param name="rootLocator">The root locator variable expression.</param>
        /// <param name="parentVariable">The parent variable.</param>
        /// <param name="itemVariable">The item variable.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="attribute">The attribute.</param>
        /// <param name="nativeAttributes">The native attributes.</param>
        /// <returns>The expressions needed to create the list</returns>
        private IEnumerable<Expression> CreateHtmlObject(ExpressionData browser, ExpressionData rootLocator, ExpressionData parentVariable, Expression itemVariable, Type propertyType, string propertyName, ElementLocatorAttribute attribute, IEnumerable nativeAttributes)
        {
            var objectType = this.GetPropertyProxyType(propertyType);

            var propConstructor = this.GetConstructor(objectType, browser, parentVariable, rootLocator);
            if (propConstructor == null)
            {
                throw this.CreateConstructorException(propertyName, objectType);
            }

            return new[]
				       {
					       (Expression)Expression.Assign(itemVariable, Expression.New(propConstructor.Item1, propConstructor.Item2)),
						   Expression.Call(Expression.Constant(this), 
                                           this.assignMethodInfo,
                                           Expression.Convert(itemVariable, typeof(TElement)),
                                           Expression.Constant(attribute, typeof(ElementLocatorAttribute)),
                                           Expression.Constant(nativeAttributes, typeof(object[])))
				       };
        }

        /// <summary>
        /// An smart structure for managing expressions and matching types.
        /// </summary>
        protected class ExpressionData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExpressionData"/> class.
            /// </summary>
            /// <param name="expression">The expression.</param>
            /// <param name="type">The type.</param>
            public ExpressionData(Expression expression, Type type)
            {
                this.Expression = expression;
                this.Type = type;
            }

            /// <summary>
            /// Gets the expression.
            /// </summary>
            /// <value>The expression.</value>
            public Expression Expression { get; private set; }

            /// <summary>
            /// Gets the type.
            /// </summary>
            /// <value>The type.</value>
            public Type Type { get; private set; }
        }
    }
}