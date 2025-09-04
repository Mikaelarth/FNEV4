# Données de test pour FNEV4

Ce dossier contient toutes les données nécessaires pour les tests unitaires et d'intégration.

## Structure

### `/Excel`
- Fichiers Excel de test pour le parsing Sage 100
- Exemples de structure conforme et non-conforme
- Cas d'erreur et cas limites

### `/Json`
- Réponses simulées de l'API FNE DGI
- Données de test pour les certifications
- Exemples de retours d'erreur API

### `/Mocks`
- Configuration des objets mock
- Données de test pour les entités
- Helpers pour les tests

## Convention de nommage

- **Fichiers Excel**: `test_[scenario]_[date].xlsx`
- **Fichiers JSON**: `mock_[endpoint]_[response].json`
- **Classes Mock**: `Mock[EntityName].cs`

## Utilisation

Ces données sont référencées dans les tests unitaires et d'intégration pour garantir la cohérence et la reproductibilité des tests.
