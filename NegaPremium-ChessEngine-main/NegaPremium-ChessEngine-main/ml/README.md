# Machine Learning Workspace

This folder is reserved for CNN experiments that sit beside the C# chess engine.
Keep the engine source under `Source/` unchanged, and place ML data, notebooks,
training scripts, model checkpoints, and experiment output here.

## Current dataset

- Dataset: Kaggle `krithiik/chess-pieces`
- Local path: `datasets/chess-pieces-kaggle/raw/all_resized_into_sub_folders_640`
- Task fit: supervised image classification for chess piece recognition
- Classes: 12 piece-color classes, 25 JPG images per class

The class folder is ready for common CNN loaders:

- TensorFlow/Keras: `tf.keras.utils.image_dataset_from_directory(...)`
- PyTorch: `torchvision.datasets.ImageFolder(...)`

Generated outputs such as trained models, run logs, processed data, and train
splits should be written under ignored folders like `ml/models/`, `ml/runs/`,
or `ml/datasets/**/processed/`.
