# PROCÉDURE D'INTERFAÇAGE DES ENTREPRISES PAR API

**Direction Générale des Impôts (DGI)**  
**Mai 2025**  
**Ce document contient 26 pages**

---

## DÉFINITION ET ABRÉVIATIONS

- **API** : Application Programming Interface
- **DGI** : Direction Générale des Impôts
- **FNE** : Facture Normalisée Électronique
- **TERNE** : Terminaux d'Émission de Reçus Normalisés Électroniques

---

## I- CONTEXTE

La loi de finances pour la gestion 2025 a institué l'obligation de délivrance de la facture normalisée électronique (FNE) et du reçu normalisé électronique (RNE), reprenant ainsi les dispositions de 2005 en ses articles 384, 385 et suivants du code général des impôts (CGI) et en ces articles 144 et suivants du livre de procédures fiscales (LPF).

La facture normalisée électronique constitue une évolution du système de facturation en Côte d'Ivoire. La nouvelle mesure ne supprime aucune mention exigée par les anciens textes. Le principal élément nouveau de la réforme est la digitalisation et la dématérialisation des procédures.

Désormais, en plus des mentions habituelles, les factures doivent être issues d'un process prenant en compte les dispositions suivantes :

- **Génération de la facture électronique**, soit par l'utilisation directe du module de facturation électronique ou par l'application mobile (sur téléphone), soit par l'interfaçage (API) du module de facturation électronique avec le système de facturation des entreprises, soit par le biais de Terminaux d'Émission de Reçus Normalisés Électroniques (TERNE) ;

- **Certification par un sticker électronique** qui se traduit par l'apposition d'une signature électronique en trois éléments (le QR Code, le visuel FNE et le format de la numérotation) ;

- **Numérotation en série ininterrompue** annuelle des factures et reçus électroniques émis.

---

## II- OBJET ET DOMAINE D'APPLICATION

En plus du droit commun, il est prévu, sur option, une disposition particulière pour les entreprises qui disposent de leur propre système informatisé de facturation. Elles sont autorisées à générer leurs propres factures et à les faire certifier par la plateforme FNE, par API (Application Programming Interface).

La présente procédure a pour objet de définir les modalités de mise en œuvre de ce dispositif d'interfaçage technique entre le système de facturation de l'entreprise et la plateforme FNE.

---

## III- PROCÉDURE À SUIVRE POUR L'INTERFAÇAGE

- Inscription obligatoire de l'entreprise à la plateforme FNE de l'environnement test via le lien : http://54.247.95.108 ;
- Configuration et paramétrage de l'environnement de test de l'entreprise ;
- Développement pour l'interfaçage avec l'API ;
- Réalisation des tests de génération des factures (vente, avoir, bordereau) ;
- Transmission des spécimens de factures par l'entreprise à la DGI via le mail support.fne@dgi.gouv.ci ;
- Validation de la conformité des factures transmises, par la DGI ;
- Transmission par la DGI de l'URL de production ;
- Affichage de la clé API sur l'espace de l'entreprise dans l'onglet « Paramétrage ».

---

## IV- MODALITÉS PRATIQUES – DESCRIPTION DE LA PROCÉDURE

Toute entreprise souhaitant effectuer l'interfaçage entre son logiciel de facturation et la plateforme de Facture Normalisée Électronique (FNE), doit s'assurer que son logiciel répond aux critères suivants :

- Supporter les requêtes HTTP (RESTful API) ;
- Gérer des données JSON ;
- Supporter l'authentification via OAuth 2.0 ou certificat d'authentification ;
- Disposer d'une connexion à internet stable et sécurisée.

### Les différentes étapes à suivre sont :

### 1. Étape 1 : Accès à l'environnement de test

L'entreprise doit procéder aux actions ci-dessous :

**• Inscription à la FNE** : l'entreprise procède à son inscription sur la plateforme FNE via le module en ligne à l'adresse : http://54.247.95.108 ;

**• Inscription et configuration de l'environnement test** : l'entreprise a accès au lien de l'environnement test dont elle effectue les paramétrages et les configurations nécessaires.

**Livrable(s) :**
- Documentation technique ;
- URL de l'environnement test.

### 2. Étape 2 : Développement et test

L'entreprise doit procéder aux actions ci-dessous :

**• Développement** : l'entreprise procède aux développements nécessaires pour interfacer son système de facturation à l'API ;

**• Test en environnement de test** : une fois les développements terminés, l'entreprise procède aux tests de génération et de certification des factures.

### 3. Étape 3 : Transmission des factures à la DGI et validation de l'interfaçage

**L'entreprise :**

**• Transmission des spécimens de factures** : une fois les tests validés, l'entreprise transmet les spécimens de factures générés par la plateforme FNE et les factures correspondantes dans son système de facturation à la DGI via l'adresse mail support.fne@dgi.gouv.ci

**Livrable(s) :** spécimens de factures.

**La DGI :**

**• Réception et analyse des factures** : la DGI accuse réception des spécimens de factures transmis par l'entreprise et procède à l'analyse des évidences (conformité de fonds et de forme) ;

**• Validation de l'interfaçage** : une fois que les factures transmises sont conformes, la DGI adresse à l'entreprise un mail d'information du succès de l'interfaçage. La DGI transmet par ailleurs l'URL de production et valide l'accès de l'entreprise à la clé API via son espace FNE.

**Livrable(s) :**
- Spécimens de factures ;
- URL de production.

---

## V- DESCRIPTION DE L'API

### 1. Format de données

L'API prend en charge le format de données JSON, tant dans les demandes que dans les réponses.

Les en-têtes de demande sont définis comme suit :

| Request header | Valeur | Description |
|----------------|---------|-------------|
| content-type | application/json | La demande contient des données JSON |
| accept | application/json | La réponse doit être au format JSON |

### 2. Authentification

L'authentification est fournie par le biais d'un jeton JWT qui doit être inclus dans l'en-tête de demande.

| Request header | Valeur | Description |
|----------------|---------|-------------|
| Authorization | Bearer <token> | <token> a la valeur du jeton fourni par la FNE |

Les demandes non autorisées retourneront le code d'état HTTP 401 Unauthorized.

### 3. API methods

L'API est RESTful et utilise la méthode POST.

Toute demande POST doit contenir le corps de contenu, sinon l'API retournera le code d'état HTTP 400.

### 4. Process flow pour la certification des factures des entreprises via ERP APIs

L'API a trois parties :
- Certification de facture de vente ;
- Certification de facture d'avoir ;
- Certification du bordereau d'achat de produits agricoles.

**URL test :** http://54.247.95.108/ws  
**URL prod :** transmission de l'URL de production après validation de l'intégration par la DGI.

L'utilisation des APIs nécessite au préalable une authentification de type « Bearer Token ». Pour l'endpoint il vous suffira dans la section « Authorization » de sélectionner le type d'authentification et d'entrer la valeur API KEY située dans la section Paramétrage de votre espace FNE. Il faut noter que cette valeur n'est visible que par le gestionnaire principal et après validation par la DGI.

**Exemple :**
```
Authorization: Bearer kAF01gEM40r1Uz5WLJn5lxAnGMwVjCME
```

---

## API #1 : Certification de facture de vente

### Endpoint
**POST :** $url/external/invoices/sign

### Paramètres

| Paramètre | Format | Description | Obligatoire |
|-----------|---------|-------------|-------------|
| invoiceType | string | Type de facture (vente, bordereau d'achat) | O |
| paymentMethod | string | Méthode de paiement | O |
| template | string | Type de facturation (B2C, B2G, B2B, B2F) | O |
| isRne | boolean | Est-ce que la facture est reliée à un reçu (true or false) | O |
| rne | string | Numéro du reçu pour lequel la facture est émise | O si isRne est vrai |
| clientNcc | string | NCC du client | Obligatoire si Template est B2B |
| clientCompanyName | string | Nom du client | O |
| clientPhone | int | Numéro de téléphone du client | O |
| clientEmail | string | E-mail du client | O |
| clientSellerName | string | Nom du vendeur | N |
| pointOfSale | string | Nom du point de vente | O |
| establishment | string | Nom de l'établissement | O |
| commercialMessage | string | Message commercial | N |
| footer | string | Message personnel | N |
| foreignCurrency | string | Monnaie étrangère | N |
| foreignCurrencyRate | number | Taux de la monnaie étrangère | O si foreignCurrency n'est pas vide, 0 si foreignCurrency est null |
| items | Array | Liste des articles | O |
| taxes | string | Type de TVA (TVA, TVAB, TVAC, TVAD) | O |
| customTaxes | Array | Autres taxes | N |
| name | string | Nom de l'autre taxe | O si customTaxes n'est pas vide |
| amount | number | Taux de L'autre taxe | O si customTaxes n'est pas vide |
| reference | string | Référence de l'article | N |
| description | string | Désignation de l'article | O |
| quantity | number | Quantité | O |
| amount | number | Prix unitaire HT | O |
| discount | number | Remise sur article | N |
| measurementUnit | string | Unit de mesure des articles | N |
| discount | number | Remise sur le total HT | N |

**Il faut noter que les champs foreignCurrency et foreignCurrencyRate sont Obligatoires pour la transaction B2F avec des devises différentes.**

### Exemple de requête

**Headers**
- Content-Type:application/json

**Body**
```json
{
  "invoiceType": "sale",
  "paymentMethod": "mobile-money",
  "template": "B2B",
  "clientNcc": "9502363N",
  "clientCompanyName": "KPMG COTE D'VOIRE",
  "clientPhone": "0709080765",
  "clientEmail": "info@kpmg.ci",
  "clientSellerName": "Ali Hassan",
  "pointOfSale": "23",
  "establishment": "Orange Riviera Mpouto",
  "commercialMessage": "Soyez les bienvenus",
  "footer": "Toujours la pour votre bonheur",
  "foreignCurrency": "",
  "foreignCurrencyRate": 0,
  "items": [
    {
      "taxes": ["TVA"],
      "customTaxes": [
        {
          "name": "GRA",
          "amount": 5
        }
      ],
      "reference": "ref009",
      "description": "sac de riz Dinor 5 x 5",
      "quantity": 30,
      "amount": 20000,
      "discount": 10,
      "measurementUnit": "pcs"
    },
    {
      "taxes": ["TVAC"],
      "customTaxes": [
        {
          "name": "AIRSI",
          "amount": 2
        }
      ],
      "reference": "ref001",
      "description": "Huile lesieur 5 litres",
      "quantity": 20,
      "amount": 12000,
      "discount": 10,
      "measurementUnit": "bidon"
    }
  ],
  "customTaxes": [
    {
      "name": "DTD",
      "amount": 5
    }
  ],
  "discount": 10
}
```

### Réponse

**Code 200 : Succès**

**Headers**
- Content-Type:application/json

**Body**
```json
{
  "ncc": "9606123E",
  "reference": "9606123E25000000019",
  "token": "http://54.247.95.108/fr/verification/019465c1-3f61-766c-9652-706e32dfb436",
  "warning": false,
  "balance_sticker": 179,
  "invoice": {
    "id": "e2b2d8da-a532-4c08-9182-f5b428ca468d",
    "parentId": null,
    "parentReference": null,
    "token": "019465c1-3f61-766c-9652-706e32dfb436",
    "reference": "9606123E25000000019",
    "type": "invoice",
    "subtype": "normal",
    "date": "2025-01-14T16:59:11.016Z",
    "paymentMethod": "mobile-money",
    "amount": 852660,
    "vatAmount": 172260,
    "fiscalStamp": 0,
    "discount": 10,
    "clientNcc": "9502363N"
    // ... suite des données de la facture
  }
}
```

| Paramètre | Format | Description | Obligatoire |
|-----------|---------|-------------|-------------|
| ncc | string | Identifiant contribuable | N/A |
| reference | string | Numéro de la facture | N/A |
| token | string | Code de vérification à convertir en QR code | N/A |
| warning | string | Alerte sur le stock de sticker | N/A |
| balance_sticker | int | Balance sticker facture | N/A |
| invoice | object | Informations de la facture générée | N/A |

---

## API #2 : Certification de facture d'avoir

### Endpoint
**POST:** $url/external/invoices/{id}/refund

### Paramètres

| Paramètre | Format | Description | Obligatoire |
|-----------|---------|-------------|-------------|
| id | string | L'identifiant de la facture d'origine doit être récupéré dans la réponse de la requête de certification, puis transmis dans l'appel à l'endpoint correspondant. | Y |
| items | Array | Liste des articles | Y |
| id | string | id de l'article sur lequel on veut faire un avoir | Y |
| quantity | number | La quantité de l'article à retourner | Y |

### Exemple de requête

**Body**
```json
{
  "items": [
    {
      "id": "50b5c9d9-e22d-4dce-ba3c-5d2519c3418f",
      "quantity": 10
    },
    {
      "id": "bf9cc241-9b5f-4d26-a570-aa8e682a759e",
      "quantity": 20
    }
  ]
}
```

### Exemple de requête Curl
```bash
curl --location 'http://54.247.95.108/ws/external/invoices/e2b2d8da-a532-4c08-9182-f5b428ca468d/refund' \
--header 'Content-Type: application/json' \
--header 'Accept: application/json' \
--header 'Authorization: ••••••' \
--data '{
  "items": [
    {
      "id": "bf9cc241-9b5f-4d26-a570-aa8e682a759e",
      "quantity": 20
    },
    {
      "id": "50b5c9d9-e22d-4dce-ba3c-5d2519c3418f",
      "quantity": 10
    }
  ]
}'
```

### Réponses

**Code 201 : Succès**
```json
{
  "ncc": "9606123E",
  "reference": "A9606123E2500000006",
  "token": "http://54.247.95.108/fr/verification/019465ca-c27c-700e-ba3f-09d0759b9170",
  "warning": false,
  "balance_sticker": 178
}
```

| Paramètre | Format | Description | Obligatoire |
|-----------|---------|-------------|-------------|
| ncc | string | Identifiant contribuable | N/A |
| reference | string | Numéro de la facture | N/A |
| token | string | Code de vérification à convertir en QR code | N/A |
| warning | string | Alerte sur le stock de sticker | N/A |
| balance sticker | int | Balance sticker facture | N/A |

**Code 401: Erreur authentification**
```json
{
  "message": "Unauthorized",
  "error": "unauthorized_exception",
  "statusCode": 401
}
```

**Code 500 : Endpoint non accessible**
```json
{
  "message": "Internal Server Error",
  "error": "internal_server_error",
  "statusCode": 500
}
```

---

## API #3 : Certification du bordereau d'achat de produits agricoles

### Endpoint
**POST :** $url/external/invoices/sign

### Paramètres

| Paramètre | Format | Description | Obligatoire |
|-----------|---------|-------------|-------------|
| invoiceType | string | Type de facture (purchase) | O |
| paymentMethod | string | Méthode de paiement | O |
| template | string | Type de facturation (B2C, B2G, B2B, B2F) | O |
| isRne | boolean | Est-ce que la facture est reliée à un reçu (true or false) | O |
| rne | string | Numéro du reçu pour lequel la facture est émise | O si Rne est vrai |
| clientCompanyName | string | Nom du fournisseur | O |
| clientPhone | int | Numéro de téléphone du fournisseur | O |
| clientEmail | string | E-mail du fournisseur | O |
| clientSellerName | string | Nom du vendeur | N |
| pointOfSale | string | Nom du point de vente | O |
| establishment | string | Nom de l'établissement | O |
| commercialMessage | string | Message commercial | N |
| footer | string | Message personnel | N |
| items | Array | Liste des articles | O |
| reference | string | Référence de l'article | N |
| description | string | Désignation de l'article | O |
| quantity | number | Quantité | O |
| amount | number | Prix unitaire HT | O |
| discount | number | Remise sur article | N |
| measurementUnit | string | Unité de mesure des article | N |
| Discount | number | Remise sur le total HT | N |

### Exemple de requête

**Headers**
- Content-Type:application/json

**Body**
```json
{
  "invoiceType": "purchase",
  "paymentMethod": "mobile-money",
  "template": "B2B",
  "clientCompanyName": "COOPERATION DU GRAND OUEST",
  "clientPhone": "0709080765",
  "clientEmail": "info@cgo.ci",
  "clientSellerName": "Ali Hassan",
  "pointOfSale": "23",
  "establishment": "Orange Riviera Mpouto",
  "commercialMessage": "Soyez les bienvenus",
  "footer": "Toujours la pour votre bonheur",
  "foreignCurrency": "",
  "foreignCurrencyRate": "",
  "items": [
    {
      "reference": "ref009",
      "description": "sac de riz Dinor 5 x 5",
      "quantity": 30,
      "amount": 20000,
      "discount": 10,
      "measurementUnit": "pcs"
    },
    {
      "reference": "ref001",
      "description": " Cacao Brut premier choix",
      "quantity": 2000,
      "amount": 2200,
      "discount": 10,
      "measurementUnit": "bidon"
    }
  ],
  "discount": 10
}
```

### Réponse

**Code 200 : Succès**

**Headers**
- Content-Type:application/json

**Body**
```json
{
  "ncc": "9606123E",
  "reference": "9606123E25000000019",
  "token": "http://54.247.95.108/fr/verification/019465c1-3f61-766c-9652-706e32dfb436",
  "warning": false,
  "balance_sticker": 179,
  "invoice": {
    "id": "e2b2d8da-a532-4c08-9182-f5b428ca468d",
    "parentId": null,
    "parentReference": null,
    "token": "019465c1-3f61-766c-9652-706e32dfb436",
    "reference": "9606123E25000000019",
    "type": "invoice",
    "subtype": "normal",
    "date": "2025-01-14T16:59:11.016Z",
    "paymentMethod": "mobile-money",
    "amount": 852660,
    "vatAmount": 172260
    // ... suite des données
  }
}
```

### Exemples de réponses d'erreur

**Code 400 : Erreur dans la requête**

**Headers**
- Content-Type:application/json

**Body**
```json
{
  "message": "Point of sale is not valid",
  "error": "bad_request",
  "statusCode": 400
}
```

| Paramètre | Format | Description | Obligatoire |
|-----------|---------|-------------|-------------|
| message | string | Message d'erreur | N/A |
| error | string | Error | N/A |
| statusCode | number | statut code | N/A |
| errors | string | Détails d'erreur | N/A |

**Code 401 : erreur d'authentification**

**Headers**
- Content-Type:application/json

**Body**
```json
{
  "message": "Invalid API Key",
  "error": "unauthorized_exception",
  "statusCode": 401
}
```

---

## 5. Tests et Validation

- Effectuer des tests unitaires sur des cas de validation et d'erreur ;
- Valider l'envoi et la réception des données sur la plateforme FNE ;
- Corriger les erreurs identifiées avant la mise en production.

## 6. Mise en Production

- **Passage en environnement de production** : une fois que les tests seront validés au niveau de l'environnement de test, l'URL de production sera transmise par la DGI après validation des tests ;
- **Assurer une surveillance des transactions API** pour détecter d'éventuelles anomalies ;

## 7. Support et Maintenance

- Surveillance des logs et gestion des erreurs ;
- Mise à jour des accès et des paramètres si nécessaire ;
- Documentation et formation des utilisateurs internes.

## 8. Contact et Assistance

Pour toute assistance, contacter le support technique de la plateforme FNE à l'adresse : **support.fne@dgi.gouv.ci**

Cette procédure assure une intégration efficace et sécurisée de l'API entre les ERP des entreprises et la plateforme FNE. Veuillez suivre chaque étape attentivement pour garantir le bon fonctionnement du système.

---

## VI- ANNEXES

### Annexe 1 : Lexique

| Paramètre | Format | Valeurs possible | Obligatoire |
|-----------|---------|------------------|-------------|
| invoiceType | string | sale : vente, purchase : bordereau d'achat | O |
| paymentMethod | string | cash : espèce, card : carte bancaire, check : cheque, mobile-money : mobile money, transfer : virement bancaire, deferred : à terme | O |
| template | string | B2B : Le client est l'entreprise ou professionnel possédant un NCC, B2F : le client est à l'international, B2G : le client est une institution gouvernementale, B2C : le client est un particulier | O |
| isRne | boolean | true : vrai, false : faux | O |
| foreignCurrency | string | XOF : Franc CFA, USD : Dollar American, EUR : Euro, JPY : Yen Japonais, CAD : Dollar Canadien, GBP : livre sterling Britannique, AUD : Dollar Australien, CNH : Yuan chinois, CHF : Franc Suisse, HKD : Dollar Hong Kong, NZD : Dollar Neo-Zelandais | N |
| taxes | string | TVA : TVA normal de 18%, TVAB : TVA réduit de 9%, TVAC : TVA exec conv de 0%, TVAD : TVA exec leg de 0% pour TEE et RME | O |

### Annexe 2 : Codes erreur

| Code | Description |
|------|-------------|
| 200, 201 | Succès |
| 400 | Erreur dans la requête |
| 401 | Erreur d'authentification |
| 500 | Endpoint non disponible |