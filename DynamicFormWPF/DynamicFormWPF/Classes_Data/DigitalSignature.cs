// -----------------------------------------------------------------------
// <copyright file="DigitalSignature.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Xml;
    using System.Xml;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    /// Sign and verify the Digital Signatures of XML Documents
    /// http://msdn.microsoft.com/en-us/library/ms229745(v=vs.100).aspx
    public class DigitalSignature
    {
        public static string signXML(string xmlFilePath)
        {
            string info = string.Empty;

            try
            {
                // Create a new CspParameters object to specify
                // a key container.
                CspParameters cspParams = new CspParameters();
                cspParams.KeyContainerName = "XML_SEIS_RSA_KEY";

                // Create a new RSA signing key and save it in the container.
                RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(cspParams);

                // save the key pair
                string strRSAKeyXML = rsaKey.ToXmlString(true);
                XmlDocument XMLRSAKeyPair = new XmlDocument();
                XMLRSAKeyPair.LoadXml(strRSAKeyXML);
                XMLRSAKeyPair.Save("XMLSignatureRSAKeyPair.xml");

                // Create a new XML document.
                XmlDocument xmlDoc = new XmlDocument();

                // Load an XML file into the XmlDocument object.
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlFilePath);

                // Sign the XML document.
                SignXml(xmlDoc, rsaKey);

                //Console.WriteLine("XML file signed.");
                info = "OK";

                // Save the document.
                xmlDoc.Save(xmlFilePath);
            }
            catch (Exception e)
            {
                info = e.Message;
            }
            return info;
        }

        // Sign an XML file.
        // This document cannot be verified unless the verifying
        // code has the key with which it was signed.
        private static void SignXml(XmlDocument xmlDoc, RSA Key)
        {
            // Check arguments.
            if (xmlDoc == null)
                throw new ArgumentException("xmlDoc");
            if (Key == null)
                throw new ArgumentException("Key");

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(xmlDoc);

            // Add the key to the SignedXml document.
            signedXml.SigningKey = Key;

            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "";

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
        }

        public static string verifyXML(string xmlFilePath)
        {
            string info = string.Empty;
            try
            {
                // Create a new CspParameters object to specify
                // a key container.
                //CspParameters cspParams = new CspParameters();
                //cspParams.KeyContainerName = "XML_SEIS_RSA_KEY";

                // Create a new RSA signing key and save it in the container.
                //RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(cspParams);
                RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider();

                // load the key pair file
                XmlDocument objKeyPairXMLdocument = new XmlDocument();
                objKeyPairXMLdocument.Load("XMLSignatureRSAKeyPair.xml");
                string strRSAKeyPairXML = objKeyPairXMLdocument.InnerXml;

                rsaKey.FromXmlString(strRSAKeyPairXML);

                // Create a new XML document.
                XmlDocument xmlDoc = new XmlDocument();

                // Load an XML file into the XmlDocument object.
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.Load(xmlFilePath);

                // Verify the signature of the signed XML.
                bool result = VerifyXml(xmlDoc, rsaKey);

                // Display the results of the signature verification to
                // the console.
                if (result)
                {
                    //Console.WriteLine("The XML signature is valid.");
                    info = "OK";
                }
                else
                {
                    //Console.WriteLine("The XML signature is not valid.");
                    info = "NotOK";
                }
            }
            catch (Exception e)
            {
                info = e.Message;
            }
            return info;
        }

        // Verify the signature of an XML file against an asymmetric
        // algorithm and return the result.
        private static Boolean VerifyXml(XmlDocument Doc, RSA Key)
        {
            // Check arguments.
            if (Doc == null)
                throw new ArgumentException("Doc");
            if (Key == null)
                throw new ArgumentException("Key");

            // Create a new SignedXml object and pass it
            // the XML document class.
            SignedXml signedXml = new SignedXml(Doc);

            //RSA objRSAPublicKey = RSA.Create();
            //objRSAPublicKey.FromXmlString(objKeyPairXMLdocument);
            //KeyInfo keyInfo = new KeyInfo();
            //keyInfo.AddClause(new RSAKeyValue(objRSAPublicKey));
            //signedXml.KeyInfo = keyInfo;

            // Find the "Signature" node and create a new
            // XmlNodeList object.
            XmlNodeList nodeList = Doc.GetElementsByTagName("Signature");

            // Throw an exception if no signature was found.
            if (nodeList.Count <= 0)
            {
                throw new CryptographicException("Verification failed: No Signature was found in the document.");
            }

            // This example only supports one signature for
            // the entire XML document.  Throw an exception
            // if more than one signature was found.
            if (nodeList.Count >= 2)
            {
                throw new CryptographicException("Verification failed: More that one signature was found for the document.");
            }

            // Load the first <signature> node.
            signedXml.LoadXml((XmlElement)nodeList[0]);

            // Check the signature and return the result.
            return signedXml.CheckSignature(Key);
        }
    }
}